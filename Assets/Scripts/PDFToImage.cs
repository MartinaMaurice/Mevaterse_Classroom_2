using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class PDFToImage
{
    [DllImport("pdfium")]
    public static extern IntPtr FPDF_LoadDocument(string file_path, string password);

    [DllImport("pdfium")]
    public static extern void FPDF_CloseDocument(IntPtr document);

    [DllImport("pdfium")]
    public static extern int FPDF_GetPageCount(IntPtr document);

    [DllImport("pdfium")]
    public static extern IntPtr FPDF_LoadPage(IntPtr document, int page_index);

    [DllImport("pdfium")]
    public static extern void FPDF_ClosePage(IntPtr page);

    [DllImport("pdfium")]
    public static extern void FPDF_RenderPage(IntPtr bitmap, IntPtr page, int start_x, int start_y, int size_x, int size_y, int rotate, int flags);

    [DllImport("pdfium")]
    public static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);

    [DllImport("pdfium")]
    public static extern void FPDFBitmap_Destroy(IntPtr bitmap);

    [DllImport("pdfium")]
    public static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);

    [DllImport("pdfium")]
    public static extern void FPDF_InitLibrary();

    [DllImport("pdfium")]
    public static extern void FPDF_DestroyLibrary();

    // Call this method before using PDFium
    public static void Initialize()
    {
        FPDF_InitLibrary();
    }

    // Call this method to free resources
    public static void Destroy()
    {
        FPDF_DestroyLibrary();
    }

    // Render a specific page to a Texture2D
    public static Texture2D RenderPage(string pdfPath, int pageNumber, int width = 800, int height = 800)
    {
        IntPtr document = FPDF_LoadDocument(pdfPath, null);
        if (document == IntPtr.Zero)
        {
            Debug.LogError("Failed to load PDF document.");
            return null;
        }

        IntPtr page = FPDF_LoadPage(document, pageNumber);
        if (page == IntPtr.Zero)
        {
            FPDF_CloseDocument(document);
            Debug.LogError("Failed to load PDF page.");
            return null;
        }

        // Create a bitmap in PDFium
        IntPtr bitmap = FPDFBitmap_Create(width, height, 1); // 1 enables alpha channel
        int renderFlags = 0x10 | 0x20 | 0x800; // FPDF_ANNOT | FPDF_LCD_TEXT | FPDF_NO_CATCH

        // Render the page to the bitmap with additional render flags
        FPDF_RenderPage(bitmap, page, 0, 0, width, height, 0, renderFlags);

        // Get bitmap buffer and convert to Texture2D
        IntPtr buffer = FPDFBitmap_GetBuffer(bitmap);
        byte[] imageBytes = new byte[width * height * 4]; // RGBA format
        Marshal.Copy(buffer, imageBytes, 0, imageBytes.Length);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(imageBytes);
        texture.Apply();

        // Clean up resources
        FPDFBitmap_Destroy(bitmap);
        FPDF_ClosePage(page);
        FPDF_CloseDocument(document);

        return texture;
    }

}
