﻿using System.Drawing;
using System.IO;
using System.IO.Compression;

namespace Nikse.SubtitleEdit.Logic
{
    public class ManagedBitmap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private Color[] _colors;

        public ManagedBitmap(string fileName)
        {
            byte[] buffer = new byte[1024];
            using (MemoryStream fd = new MemoryStream())
            using (Stream fs = File.OpenRead(fileName))
            using (Stream csStream = new GZipStream(fs, CompressionMode.Decompress))
            {
                int nRead;
                while ((nRead = csStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fd.Write(buffer, 0, nRead);
                }
                csStream.Flush();
                csStream.Close();
                buffer = fd.ToArray();
            }

            Width = buffer[4] << 8 | buffer[5];
            Height = buffer[6] << 8 | buffer[7];
            _colors = new Color[Width * Height];
            int start = 8;
            for (int i = 0; i < _colors.Length; i++)
            {
                _colors[i] = Color.FromArgb(buffer[start], buffer[start + 1], buffer[start + 2], buffer[start + 3]);
                start += 4;
            }
        }

        public ManagedBitmap(Stream stream)
        {
            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, buffer.Length);
            Width = buffer[4] << 8 | buffer[5];
            Height = buffer[6] << 8 | buffer[7];
            _colors = new Color[Width * Height];
            buffer = new byte[Width * Height * 4];
            stream.Read(buffer, 0, buffer.Length);
            int start = 0;
            for (int i = 0; i < _colors.Length; i++)
            {
                _colors[i] = Color.FromArgb(buffer[start], buffer[start + 1], buffer[start + 2], buffer[start + 3]);
                start += 4;
            }
        }

        public ManagedBitmap(Bitmap oldBitmap)
        {
            NikseBitmap nbmp = new NikseBitmap(oldBitmap);
            Width = nbmp.Width;
            Height = nbmp.Height;
            _colors = new Color[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    this.SetPixel(x, y, nbmp.GetPixel(x, y));
                }
            }
        }

        public ManagedBitmap(NikseBitmap nbmp)
        {
            Width = nbmp.Width;
            Height = nbmp.Height;
            _colors = new Color[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    this.SetPixel(x, y, nbmp.GetPixel(x, y));
                }
            }
        }

        public void Save(string fileName)
        {
            using (MemoryStream outFile = new MemoryStream())
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes("MBMP");
                outFile.Write(buffer, 0, buffer.Length);
                WriteInt16(outFile, (short)Width);
                WriteInt16(outFile, (short)Height);
                foreach (Color c in _colors)
                {
                    WriteColor(outFile, c);
                }
                buffer = outFile.ToArray();
                using (FileStream f2 = new FileStream(fileName, FileMode.Create))
                using (GZipStream gz = new GZipStream(f2, CompressionMode.Compress, false))
                {
                    gz.Write(buffer, 0, buffer.Length);
                    gz.Flush();
                    gz.Close();
                }
            }
        }

        public void AppendToStream(Stream targetStream)
        {
            using (MemoryStream outFile = new MemoryStream())
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes("MBMP");
                outFile.Write(buffer, 0, buffer.Length);
                WriteInt16(outFile, (short)Width);
                WriteInt16(outFile, (short)Height);
                foreach (Color c in _colors)
                {
                    WriteColor(outFile, c);
                }
                buffer = outFile.ToArray();
                targetStream.Write(buffer, 0, buffer.Length);
            }
        }

        private int ReadInt16(Stream stream)
        {
            byte b0 = (byte)stream.ReadByte();
            byte b1 = (byte)stream.ReadByte();
            return b0 << 8 | b1;
        }

        private void WriteInt16(Stream stream, short val)
        {
            byte[] buffer = new byte[2];
            buffer[0] = (byte)((val & 0xFF00) >> 8);
            buffer[1] = (byte)(val & 0x00FF);
            stream.Write(buffer, 0, buffer.Length);
        }

        private Color ReadColor(Stream stream)
        {
            return Color.FromArgb((byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte());
        }

        private void WriteColor(Stream stream, Color c)
        {
            byte[] buffer = new byte[4];
            buffer[0] = (byte)c.A;
            buffer[1] = (byte)c.R;
            buffer[2] = (byte)c.G;
            buffer[3] = (byte)c.B;
            stream.Write(buffer, 0, buffer.Length);
        }

        public ManagedBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            _colors = new Color[Width * Height];
        }

        public Color GetPixel(int x, int y)
        {
            return _colors[Width * y + x];
        }

        public void SetPixel(int x, int y, Color c)
        {
            _colors[Width * y + x] = c;
        }

        /// <summary>
        /// Copies a rectangle from the bitmap to a new bitmap
        /// </summary>
        /// <param name="section">Source rectangle</param>
        /// <returns>Rectangle from current image as new bitmap</returns>
        public ManagedBitmap GetRectangle(Rectangle section)
        {
            ManagedBitmap newRectangle = new ManagedBitmap(section.Width, section.Height);

            int recty = 0;
            for (int y=section.Top; y < section.Top + section.Height; y++)
            {
                int rectx = 0;
                for (int x=section.Left; x< section.Left +section.Width; x++)
                {
                    newRectangle.SetPixel(rectx, recty, this.GetPixel(x, y));
                    rectx++;
                }
                recty++;
            }
            return newRectangle;
        }

        public Bitmap ToOldBitmap()
        {
            NikseBitmap nbmp = new NikseBitmap(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    nbmp.SetPixel(x, y, this.GetPixel(x, y));
                }
            }
            return nbmp.GetBitmap();
        }


        internal void DrawImage(ManagedBitmap bmp, Point point)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                int newY = point.Y + y;
                if (newY >= 0 && newY < Height)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int newX = point.X + x;
                        if (newX >= 0 && newX < Width)
                            this.SetPixel(newX, newY, bmp.GetPixel(x, y));
                    }
                }
            }
        }

    }
}
