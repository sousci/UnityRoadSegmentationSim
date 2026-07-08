"""Fine-tune SegFormer on Unity Road Segmentation captures."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
import torch
from PIL import Image
from torch.utils.data import DataLoader, Dataset
from transformers import SegformerForSemanticSegmentation


IMAGENET_MEAN = np.asarray([0.485, 0.456, 0.406], dtype=np.float32)
IMAGENET_STD = np.asarray([0.229, 0.224, 0.225], dtype=np.float32)


class UnitySegmentationDataset(Dataset):
    def __init__(self, dataset_root: Path, split: str, image_size: int):
        self.dataset_root = dataset_root
        self.image_size = image_size
        manifest = dataset_root / f"{split}.txt"
        if not manifest.exists():
            raise FileNotFoundError(manifest)

        self.samples: list[tuple[Path, Path]] = []
        for line in manifest.read_text(encoding="utf-8").splitlines():
            if not line.strip():
                continue
            image_rel, label_rel = line.split()
            self.samples.append((dataset_root / image_rel, dataset_root / label_rel))

    def __len__(self) -> int:
        return len(self.samples)

    def __getitem__(self, index: int) -> dict[str, torch.Tensor]:
        image_path, label_path = self.samples[index]
        image = Image.open(image_path).convert("RGB").resize((self.image_size, self.image_size), Image.BILINEAR)
        label = Image.open(label_path).convert("L").resize((self.image_size, self.image_size), Image.NEAREST)

        image_array = np.asarray(image, dtype=np.float32) / 255.0
        image_array = (image_array - IMAGENET_MEAN) / IMAGENET_STD
        image_array = np.transpose(image_array, (2, 0, 1))

        label_array = np.asarray(label, dtype=np.int64)
        return {
            "pixel_values": torch.from_numpy(image_array),
            "labels": torch.from_numpy(label_array),
        }


def load_class_metadata(dataset_root: Path) -> tuple[int, dict[int, str], dict[str, int]]:
    with (dataset_root / "classes.json").open("r", encoding="utf-8") as handle:
        data = json.load(handle)

    id2label = {int(key): value for key, value in data["id2label"].items()}
    label2id = {key: int(value) for key, value in data["label2id"].items()}
    return int(data["num_classes"]), id2label, label2id


def evaluate(model, loader, device) -> float:
    if len(loader.dataset) == 0:
        return 0.0

    model.eval()
    total_loss = 0.0
    with torch.no_grad():
        for batch in loader:
            pixel_values = batch["pixel_values"].to(device)
            labels = batch["labels"].to(device)
            outputs = model(pixel_values=pixel_values, labels=labels)
            total_loss += float(outputs.loss.detach().cpu())
    return total_loss / max(1, len(loader))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--dataset", default="datasets/unity_road_segmentation")
    parser.add_argument("--base-model", default="nvidia/segformer-b0-finetuned-cityscapes-1024-1024")
    parser.add_argument("--output", default="models/segformer-b0-unity-road")
    parser.add_argument("--image-size", type=int, default=512)
    parser.add_argument("--epochs", type=int, default=5)
    parser.add_argument("--batch-size", type=int, default=2)
    parser.add_argument("--learning-rate", type=float, default=5e-5)
    args = parser.parse_args()

    dataset_root = Path(args.dataset).resolve()
    output_root = Path(args.output).resolve()
    output_root.mkdir(parents=True, exist_ok=True)

    num_labels, id2label, label2id = load_class_metadata(dataset_root)
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    model = SegformerForSemanticSegmentation.from_pretrained(
        args.base_model,
        num_labels=num_labels,
        id2label=id2label,
        label2id=label2id,
        ignore_mismatched_sizes=True,
    )
    model.to(device)

    train_dataset = UnitySegmentationDataset(dataset_root, "train", args.image_size)
    val_dataset = UnitySegmentationDataset(dataset_root, "val", args.image_size)
    train_loader = DataLoader(train_dataset, batch_size=args.batch_size, shuffle=True, num_workers=0)
    val_loader = DataLoader(val_dataset, batch_size=args.batch_size, shuffle=False, num_workers=0)

    optimizer = torch.optim.AdamW(model.parameters(), lr=args.learning_rate)

    for epoch in range(args.epochs):
        model.train()
        running_loss = 0.0
        for step, batch in enumerate(train_loader, start=1):
            pixel_values = batch["pixel_values"].to(device)
            labels = batch["labels"].to(device)

            optimizer.zero_grad(set_to_none=True)
            outputs = model(pixel_values=pixel_values, labels=labels)
            outputs.loss.backward()
            optimizer.step()

            running_loss += float(outputs.loss.detach().cpu())
            if step % 10 == 0 or step == len(train_loader):
                print(f"epoch {epoch + 1}/{args.epochs} step {step}/{len(train_loader)} loss {running_loss / step:.4f}")

        val_loss = evaluate(model, val_loader, device)
        print(f"epoch {epoch + 1} validation_loss {val_loss:.4f}")

    model.save_pretrained(output_root)
    (output_root / "training_args.json").write_text(json.dumps(vars(args), indent=2), encoding="utf-8")
    print(f"Saved fine-tuned model: {output_root}")


if __name__ == "__main__":
    main()
