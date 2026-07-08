"""Export a fine-tuned SegFormer model directory to ONNX."""

from __future__ import annotations

import argparse
from pathlib import Path

import torch
from transformers import SegformerForSemanticSegmentation


class SegformerLogitsWrapper(torch.nn.Module):
    def __init__(self, model: SegformerForSemanticSegmentation):
        super().__init__()
        self.model = model

    def forward(self, pixel_values):
        return self.model(pixel_values=pixel_values).logits


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", default="models/segformer-b0-unity-road")
    parser.add_argument("--output", default="../Inference/models/segformer-b0-unity-road/model.onnx")
    parser.add_argument("--image-size", type=int, default=512)
    args = parser.parse_args()

    model_path = Path(args.model).resolve()
    output_path = Path(args.output).resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)

    model = SegformerForSemanticSegmentation.from_pretrained(model_path)
    model.eval()
    wrapper = SegformerLogitsWrapper(model)
    wrapper.eval()

    dummy = torch.randn(1, 3, args.image_size, args.image_size)
    torch.onnx.export(
        wrapper,
        (dummy,),
        output_path,
        input_names=["pixel_values"],
        output_names=["logits"],
        dynamic_axes={
            "pixel_values": {0: "batch_size", 2: "height", 3: "width"},
            "logits": {0: "batch_size", 2: "logits_height", 3: "logits_width"},
        },
        opset_version=17,
    )
    print(f"Exported ONNX: {output_path}")


if __name__ == "__main__":
    main()
