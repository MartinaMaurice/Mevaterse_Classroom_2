from pdf2image import convert_from_path
import sys
import os

def convert_pdf_to_images(pdf_path, output_folder):
    poppler_path = os.path.join(os.path.dirname(__file__), 'poppler-24.08.0', 'Library', 'bin')
    images = convert_from_path(pdf_path, poppler_path=poppler_path)
    image_paths = []
    for i, image in enumerate(images):
        image_path = os.path.join(output_folder, f"page_{i + 1}.png")
        image.save(image_path, 'PNG')
        image_paths.append(image_path)
    return image_paths

if __name__ == "__main__":
    pdf_path = sys.argv[1]
    output_folder = sys.argv[2]
    os.makedirs(output_folder, exist_ok=True)
    image_paths = convert_pdf_to_images(pdf_path, output_folder)
    print("\n".join(image_paths))  # Only print image paths
