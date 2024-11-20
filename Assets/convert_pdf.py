import sys
import os
from pdf2image import convert_from_path

# Ensure Python can find the necessary folders
current_dir = os.path.dirname(os.path.abspath(__file__))
scripts_path = os.path.join(current_dir, '../python/Scripts')
site_packages_path = os.path.join(current_dir, '../python/Lib/site-packages')

sys.path.append(scripts_path)
sys.path.append(site_packages_path)

def convert_pdf_to_images(pdf_path, output_folder):
    """
    Converts a PDF file to images (one image per page).
    Args:
        pdf_path (str): Path to the input PDF file.
        output_folder (str): Path to save output images.
    Returns:
        list: List of file paths to the generated images.
    """
    # Path to Poppler binaries
    poppler_path = os.path.join(current_dir, '../poppler-24.08.0/Library/bin')
    
    # Convert PDF to images
    images = convert_from_path(pdf_path, poppler_path=poppler_path)
    image_paths = []

    for i, image in enumerate(images):
        image_path = os.path.join(output_folder, f"page_{i + 1}.png")
        image.save(image_path, 'PNG')
        image_paths.append(os.path.abspath(image_path))  # Store absolute paths
        print(os.path.abspath(image_path))  # Print absolute paths
    return image_paths


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python convert_pdf.py <pdf_path> <output_folder>")
        sys.exit(1)

    pdf_path = sys.argv[1]
    output_folder = sys.argv[2]

    os.makedirs(output_folder, exist_ok=True)

    try:
        image_paths = convert_pdf_to_images(pdf_path, output_folder)
        print("Generated image paths:")
        print("\n".join(image_paths))
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)
