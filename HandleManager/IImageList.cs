
using System;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable 1591

namespace SiretT.Shell.Interop
{
    [ComImportAttribute()]
    [GuidAttribute("46EB5926-582E-4017-9FDF-E8998DAA0950")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IImageList {
        [PreserveSig]
        int Add(
        IntPtr hbmImage,
        IntPtr hbmMask,
        ref int pi);

        [PreserveSig]
        int ReplaceIcon(
        int i,
        IntPtr hicon,
        ref int pi);

        [PreserveSig]
        int SetOverlayImage(
        int iImage,
        int iOverlay);

        [PreserveSig]
        int Replace(
        int i,
        IntPtr hbmImage,
        IntPtr hbmMask);

        [PreserveSig]
        int AddMasked(
        IntPtr hbmImage,
        int crMask,
        ref int pi);

        [PreserveSig]
        int Draw(
        ref IMAGELISTDRAWPARAMS pimldp);

        [PreserveSig]
        int Remove(
    int i);

        [PreserveSig]
        int GetIcon(
        int i,
        int flags,
        ref IntPtr picon);

        [PreserveSig]
        int GetImageInfo(
        int i,
        ref IMAGEINFO pImageInfo);

        [PreserveSig]
        int Copy(
        int iDst,
        IImageList punkSrc,
        int iSrc,
        int uFlags);

        [PreserveSig]
        int Merge(
        int i1,
        IImageList punk2,
        int i2,
        int dx,
        int dy,
        ref Guid riid,
        ref IntPtr ppv);

        [PreserveSig]
        int Clone(
        ref Guid riid,
        ref IntPtr ppv);

        [PreserveSig]
        int GetImageRect(
        int i,
        ref RECT prc);

        [PreserveSig]
        int GetIconSize(
        ref int cx,
        ref int cy);

        [PreserveSig]
        int SetIconSize(
        int cx,
        int cy);

        [PreserveSig]
        int GetImageCount(
    ref int pi);

        [PreserveSig]
        int SetImageCount(
        int uNewCount);

        [PreserveSig]
        int SetBkColor(
        int clrBk,
        ref int pclr);

        [PreserveSig]
        int GetBkColor(
        ref int pclr);

        [PreserveSig]
        int BeginDrag(
        int iTrack,
        int dxHotspot,
        int dyHotspot);

        [PreserveSig]
        int EndDrag();

        [PreserveSig]
        int DragEnter(
        IntPtr hwndLock,
        int x,
        int y);

        [PreserveSig]
        int DragLeave(
        IntPtr hwndLock);

        [PreserveSig]
        int DragMove(
        int x,
        int y);

        [PreserveSig]
        int SetDragCursorImage(
        ref IImageList punk,
        int iDrag,
        int dxHotspot,
        int dyHotspot);

        [PreserveSig]
        int DragShowNolock(
        int fShow);

        [PreserveSig]
        int GetDragImage(
        ref POINT ppt,
        ref POINT pptHotspot,
        ref Guid riid,
        ref IntPtr ppv);

        [PreserveSig]
        int GetItemFlags(
        int i,
        ref int dwFlags);

        [PreserveSig]
        int GetOverlayImage(
        int iOverlay,
        ref int piIndex);
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGELISTDRAWPARAMS {
        public int cbSize;
        public IntPtr himl;
        public int i;
        public IntPtr hdcDst;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int xBitmap;    // x offest from the upperleft of bitmap
        public int yBitmap;    // y offset from the upperleft of bitmap
        public int rgbBk;
        public int rgbFg;
        public int fStyle;
        public int dwRop;
        public int fState;
        public int Frame;
        public int crEffect;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGEINFO {
        public IntPtr hbmImage;
        public IntPtr hbmMask;
        public int Unused1;
        public int Unused2;
        public RECT rcImage;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int left, top, right, bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        int x;
        int y;
    }

    /*ILD_NORMAL (0x00000000)
Draws the image using the background color for the image list. If the background color is the CLR_NONE value, the image is drawn transparently using the mask.

ILD_TRANSPARENT (0x00000001)
Draws the image transparently using the mask, regardless of the background color. This value has no effect if the image list does not contain a mask.

ILD_BLEND25 (0x00000002)
Draws the image, blending 25 percent with the blend color specified by rgbFg. This value has no effect if the image list does not contain a mask.

ILD_FOCUS (0x00000002)
Same as ILD_BLEND25.

ILD_BLEND50 (0x00000004)
Draws the image, blending 50 percent with the blend color specified by rgbFg. This value has no effect if the image list does not contain a mask.

ILD_SELECTED (0x00000004)
Same as ILD_BLEND50.

ILD_BLEND (0x00000004)
Same as ILD_BLEND50.

ILD_MASK (0x00000010)
Draws the mask.

ILD_IMAGE (0x00000020)
If the overlay does not require a mask to be drawn, set this flag.

ILD_ROP (0x00000040)
Draws the image using the raster operation code specified by the dwRop member.

ILD_OVERLAYMASK (0x00000F00)
To extract the overlay image from the fStyle member, use the logical AND to combine fStyle with the ILD_OVERLAYMASK value.

ILD_PRESERVEALPHA (0x00001000)
Preserves the alpha channel in the destination.

ILD_SCALE (0x00002000)
Causes the image to be scaled to cx, cy instead of being clipped.

ILD_DPISCALE (0x00004000)
Scales the image to the current dots per inch (dpi) of the display.

ILD_ASYNC (0x00008000)*/
}
