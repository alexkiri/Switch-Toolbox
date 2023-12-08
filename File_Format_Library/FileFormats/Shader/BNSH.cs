﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using Toolbox.Library;
using Toolbox.Library.Forms;
using Toolbox.Library.IO;
using System.Windows.Forms;
using FirstPlugin.Forms;

namespace FirstPlugin
{
    public class BNSH : TreeNodeFile, IFileFormat
    {
        public FileType FileType { get; set; } = FileType.Shader;

        public bool CanSave { get; set; }
        public string[] Description { get; set; } = new string[] { "Binary Shader" };
        public string[] Extension { get; set; } = new string[] { "*.bnsh" };
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public IFileInfo IFileInfo { get; set; }

        public bool Identify(System.IO.Stream stream)
        {
            using (var reader = new Toolbox.Library.IO.FileReader(stream, true))
            {
                return reader.CheckSignature(4, "BNSH");
            }
        }

        public Type[] Types
        {
            get
            {
                List<Type> types = new List<Type>();
                return types.ToArray();
            }
        }

        public void Load(System.IO.Stream stream)
        {
            Header header = new Header();
            header.Read(new FileReader(stream), this);

            Text = header.FileName + ".bnsh";
            Console.WriteLine("Did load header " + Text);
        }
        public void Unload()
        {

        }

        public void Save(System.IO.Stream stream)
        {
        }

        public class Header
        {
            public List<ShaderVariation> ShaderVariations = new List<ShaderVariation>();

            public uint VersionMajor { get; set; }
            public uint VersionMajor2 { get; set; }
            public uint VersionMinor { get; set; }
            public uint VersionMinor2 { get; set; }
            public string FileName { get; set; }

            private void SetVersionInfo(uint Version)
            {
                VersionMajor = Version >> 24;
                VersionMajor2 = Version >> 16 & 0xFF;
                VersionMinor = Version >> 8 & 0xFF;
                VersionMinor2 = Version & 0xFF;
            }

            public void Read(FileReader reader, TreeNodeCustom node)
            {
                Console.WriteLine("reader at position: " + reader.Position);
                
                reader.ByteOrder = Syroot.BinaryData.ByteOrder.LittleEndian;

                reader.ReadSignature(4, "BNSH");
                uint padding = reader.ReadUInt32();
                uint Version = reader.ReadUInt32();
                SetVersionInfo(Version);
                ushort ByteOrderMark = reader.ReadUInt16();
                byte Alignment = reader.ReadByte();
                byte Target = reader.ReadByte();
                FileName = reader.LoadString(false, typeof(uint), null, 0);
                uint PathOffset = reader.ReadUInt32();
                uint RelocationTableOffset = reader.ReadUInt32();
                uint FileSize = reader.ReadUInt32();

                reader.Seek(0x40); //padding
                reader.ReadSignature(4, "grsc");

                uint BlockOffset = reader.ReadUInt32();
                ulong BlockSize = reader.ReadUInt64();

                reader.Seek(0x0C);
                uint VariationCount = reader.ReadUInt32();
                long VariationOffset = reader.ReadUInt32();

                Console.WriteLine("FileName:" + FileName + " FileSize:" + FileSize + " VariationCount:" + VariationCount);
                Console.WriteLine("VariationOffset: {0:x}", VariationOffset);


                reader.Seek(VariationOffset, SeekOrigin.Begin);
                for (int i = 0; i < VariationCount; i++)
                {
                    ShaderVariation var = new ShaderVariation();
                    var.Text = "Shader Variation" + i;
                    Console.WriteLine();
                    Console.WriteLine("will read Shader Variation" + i);
                    if (i == 24) {
                        Console.WriteLine("Break!");
                    }
                    var.Read(reader);
                    ShaderVariations.Add(var);
                    node.Nodes.Add(var);
                }
            }
        }
        public class ShaderVariation : TreeNodeCustom
        {
            public ShaderProgram shaderProgram;

            public void Read(FileReader reader)
            {
                long SourceProgramOffset = reader.ReadInt64();
                long unk2 = reader.ReadInt64();
                long ShaderProgramOffset = reader.ReadInt64();
                long grscOffset = reader.ReadInt64(); //Points back to grsc
                reader.Seek(0x20); //padding

                using (reader.TemporarySeek(ShaderProgramOffset + SourceProgramOffset, SeekOrigin.Begin))
                {
                    shaderProgram = new ShaderProgram();
                    shaderProgram.Read(reader);

                    if (shaderProgram.VertexShader != null)
                    {
                        shaderProgram.VertexShader.Text = "Vertex Shader";
                        Nodes.Add(shaderProgram.VertexShader);
                    }
                    if (shaderProgram.GeometryShader != null)
                    {
                        shaderProgram.GeometryShader.Text = "Geometry Shader";
                        Nodes.Add(shaderProgram.GeometryShader);
                    }
                    if (shaderProgram.FragmentShader != null)
                    {
                        shaderProgram.FragmentShader.Text = "Fragment Shader";
                        Nodes.Add(shaderProgram.FragmentShader);
                    }
                    if (shaderProgram.UnkShader != null)
                    {
                        shaderProgram.UnkShader.Text = "Unk Shader";
                        Nodes.Add(shaderProgram.UnkShader);
                    }
                    if (shaderProgram.Unk2Shader != null)
                    {
                        shaderProgram.Unk2Shader.Text = "Unk2 Shader";
                        Nodes.Add(shaderProgram.Unk2Shader);
                    }
                    if (shaderProgram.ComputeShader != null)
                    {
                        shaderProgram.ComputeShader.Text = "Compute Shader";
                        Nodes.Add(shaderProgram.ComputeShader);
                    }
                }
            }
        }
        public class ShaderProgram
        {
            public ShaderData VertexShader { get; set; }
            public ShaderData UnkShader { get; set; }
            public ShaderData Unk2Shader { get; set; }
            public ShaderData GeometryShader { get; set; }
            public ShaderData FragmentShader { get; set; }
            public ShaderData ComputeShader { get; set; }

            public void Read(FileReader reader)
            {
                byte ShaderType = reader.ReadByte();
                byte Format = reader.ReadByte();

                reader.Seek(6);
                long VertexShaderOffset = reader.ReadInt64();
                long UnkShaderOffset = reader.ReadInt64(); //Might be TessControl
                long Unk2ShaderOffset = reader.ReadInt64(); //Might be TessEvaluation
                long GeometryShaderOffset = reader.ReadInt64();
                long FragmentShaderOffset = reader.ReadInt64();
                long ComputeShaderOffset = reader.ReadInt64();
                long pos = reader.Position;
                Console.WriteLine("VertexShaderOffset:{0:x} FragmentShaderOffset:{1:x} pos:{2:x}", VertexShaderOffset, FragmentShaderOffset, pos);

                if (Format == 3)
                {
                    if (VertexShaderOffset != 0)
                    {
                        reader.Seek(VertexShaderOffset, SeekOrigin.Begin);
                        VertexShader = new ShaderSourceData();
                        VertexShader.shaderType = NSWShaderDecompile.NswShaderType.Vertex;
                        VertexShader.Format = Format;
                        VertexShader.Read(reader);
                    }
                    if (FragmentShaderOffset != 0)
                    {
                        reader.Seek(FragmentShaderOffset, SeekOrigin.Begin);
                        FragmentShader = new ShaderSourceData();
                        FragmentShader.shaderType = NSWShaderDecompile.NswShaderType.Fragment;
                        FragmentShader.Format = Format;
                        FragmentShader.Read(reader);
                    }
                }
                else
                {
                    if (VertexShaderOffset != 0)
                    {
                        reader.Seek(VertexShaderOffset, SeekOrigin.Begin);
                        VertexShader = new ShaderData();
                        VertexShader.shaderType = NSWShaderDecompile.NswShaderType.Vertex;
                        VertexShader.Format = Format;
                        VertexShader.Read(reader);
                    }
                    if (UnkShaderOffset != 0)
                    {
                        reader.Seek(UnkShaderOffset, SeekOrigin.Begin);
                        UnkShader = new ShaderData();
                        UnkShader.Read(reader);
                    }
                    if (Unk2ShaderOffset != 0)
                    {
                        reader.Seek(Unk2ShaderOffset, SeekOrigin.Begin);
                        Unk2Shader = new ShaderData();
                        Unk2Shader.Read(reader);
                    }
                    if (GeometryShaderOffset != 0)
                    {
                        reader.Seek(GeometryShaderOffset, SeekOrigin.Begin);
                        GeometryShader = new ShaderData();
                        GeometryShader.shaderType = NSWShaderDecompile.NswShaderType.Geometry;
                        GeometryShader.Format = Format;
                        GeometryShader.Read(reader);
                    }
                    if (FragmentShaderOffset != 0)
                    {
                        reader.Seek(FragmentShaderOffset, SeekOrigin.Begin);
                        FragmentShader = new ShaderData();
                        FragmentShader.shaderType = NSWShaderDecompile.NswShaderType.Fragment;
                        FragmentShader.Format = Format;
                        FragmentShader.Read(reader);
                    }
                    if (ComputeShaderOffset != 0)
                    {
                        reader.Seek(ComputeShaderOffset, SeekOrigin.Begin);
                        ComputeShader = new ShaderData();
                        ComputeShader.shaderType = NSWShaderDecompile.NswShaderType.Compute;
                        ComputeShader.Format = Format;
                        ComputeShader.Read(reader);
                    }
                }


                reader.Seek(pos, SeekOrigin.Begin);
            }
        }

        public class ShaderSourceData : ShaderData
        {
            public string[] Code;

            public override void Read(FileReader reader)
            {
                ushort CodeCount = reader.ReadUInt16();
                reader.Seek(6);
                long ShaderSizeArray = reader.ReadInt64();
                long ShaderOffsetArray = reader.ReadInt64();
                reader.Seek(8);


                Console.WriteLine(ShaderOffsetArray);
                Console.WriteLine(ShaderSizeArray);

                long[] Offsets = new long[CodeCount];
                uint[] Sizes = new uint[CodeCount];
                Code = new string[CodeCount];

                using (reader.TemporarySeek(ShaderOffsetArray, SeekOrigin.Begin))
                {
                    for (int i = 0; i < CodeCount; i++)
                        Offsets[i] = reader.ReadInt64();
                }
                using (reader.TemporarySeek(ShaderSizeArray, SeekOrigin.Begin))
                {
                    for (int i = 0; i < CodeCount; i++)
                        Sizes[i] = reader.ReadUInt32();
                }

                for (int i = 0; i < CodeCount; i++)
                {
                    using (reader.TemporarySeek(Offsets[i], SeekOrigin.Begin))
                    {
                        byte[] data = reader.ReadBytes((int)Sizes[i]);
                        Code[i] = Encoding.GetEncoding(932).GetString(data);
                    }
                }
            }

            public override void OnClick(TreeView treeview)
            {
                TextEditor editor = (TextEditor)LibraryGUI.GetActiveContent(typeof(TextEditor));
                if (editor == null)
                {
                    editor = new TextEditor();
                    editor.Dock = DockStyle.Fill;
                    LibraryGUI.LoadEditor(editor);
                }

                editor.Text = Text;
                editor.FillEditor(string.Join("", Code));
            }
        }

        public class ShaderData : TreeNodeCustom
        {
            public NSWShaderDecompile.NswShaderType shaderType;
            public List<byte[]> data = new List<byte[]>();
            public int Format;
            public ulong Address;

            public virtual void Read(FileReader reader)
            {
                Console.WriteLine("reader at position:{0:x}", reader.Position);
                reader.Seek(8);
                long ShaderOffset = reader.ReadInt64();
                long ShaderOffset2 = reader.ReadInt64();
                int ShaderSize = reader.ReadInt32();
                int ShaderSize2 = reader.ReadInt32();
                Console.WriteLine("ShaderOffset1:{0:x} 2:{1:x} Size1:{1:x} Size2:{2:x}", ShaderOffset, ShaderOffset2, ShaderSize, ShaderSize2);
                reader.Seek(0x20);

                Address = (ulong)ShaderSize2;

                using (reader.TemporarySeek(ShaderOffset, SeekOrigin.Begin))
                    data.Add(reader.ReadBytes(ShaderSize2));

                using (reader.TemporarySeek(ShaderOffset2, SeekOrigin.Begin))
                    data.Add(reader.ReadBytes(ShaderSize));

                ContextMenu = new ContextMenu();
                MenuItem export = new MenuItem("Export Shader0");
                ContextMenu.MenuItems.Add(export);
                export.Click += ExportShader0;
                MenuItem export2 = new MenuItem("Export Shader1");
                ContextMenu.MenuItems.Add(export2);
                export2.Click += ExportShader1;
            }
            private void ExportShader0(object sender, EventArgs args)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.DefaultExt = "bin";
                sfd.FileName = "shader0";
                sfd.Filter = "Supported Formats|*.bin;";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(sfd.FileName, data[0]);
                }
            }
            private void ExportShader1(object sender, EventArgs args)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.DefaultExt = "bin";
                sfd.FileName = "shader1";

                sfd.Filter = "Supported Formats|*.bin;";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(sfd.FileName, data[1]);
                }
            }
            public override void OnClick(TreeView treeview)
            {
                ShaderBinaryDisplay editor = (ShaderBinaryDisplay)LibraryGUI.GetActiveContent(typeof(ShaderBinaryDisplay));
                if (editor == null)
                {
                    editor = new ShaderBinaryDisplay();
                    editor.Dock = DockStyle.Fill;
                    LibraryGUI.LoadEditor(editor);
                }

                editor.Text = Text;
                editor.FillEditor(Utils.CombineByteArray(data.ToArray()), DecompileShader());
            }
            private string DecompileShader()
            {
                //Shader A and B usually need to be combined but atm it has some issues
                return NSWShaderDecompile.DecompileShader(shaderType,
                    Utils.SubArray(data[1], 48, (uint)data[1].Length - 48), 0);
            }
        }
    }
}