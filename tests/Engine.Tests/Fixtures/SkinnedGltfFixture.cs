using System.Text.Json;

namespace Engine.Tests.Fixtures;

internal static class SkinnedGltfFixture
{
    /// <summary>Two-joint skinned triangle + optional second animation clip / rotation / scale tracks.</summary>
    public static string WriteTwoBoneSkinned(
        string dir,
        string stem,
        int animationCount = 1,
        bool withRotation = false,
        bool withScale = false)
    {
        Directory.CreateDirectory(dir);
        var binPath = Path.Combine(dir, $"{stem}.bin");
        var gltfPath = Path.Combine(dir, $"{stem}.gltf");
        var anim2 = animationCount >= 2;

        var views = WriteTwoBoneBin(binPath, anim2, withRotation, withScale);
        var binLen = new FileInfo(binPath).Length;

        var accessors = new List<object>
        {
            new
            {
                bufferView = 0, componentType = 5126, count = 3, type = "VEC3",
                max = new[] { 1f, 1f, 0f }, min = new[] { 0f, 0f, 0f }
            },
            new { bufferView = 1, componentType = 5121, count = 3, type = "VEC4" },
            new { bufferView = 2, componentType = 5126, count = 3, type = "VEC4" },
            new { bufferView = 3, componentType = 5123, count = 3, type = "SCALAR" },
            new { bufferView = 4, componentType = 5126, count = 2, type = "MAT4" },
            new
            {
                bufferView = 5, componentType = 5126, count = 2, type = "SCALAR",
                max = new[] { 1f }, min = new[] { 0f }
            },
            new { bufferView = 6, componentType = 5126, count = 2, type = "VEC3" }
        };

        if (anim2)
        {
            accessors.Add(new
            {
                bufferView = 7, componentType = 5126, count = 2, type = "SCALAR",
                max = new[] { 2f }, min = new[] { 0f }
            });
            accessors.Add(new { bufferView = 8, componentType = 5126, count = 2, type = "VEC3" });
        }

        var bufferViews = new List<object>(views.Count);
        foreach (var (offset, length) in views)
            bufferViews.Add(new { buffer = 0, byteOffset = offset, byteLength = length });

        var clipAChannels = new List<object>
        {
            new { sampler = 0, target = new { node = 1, path = "translation" } }
        };
        var clipASamplers = new List<object>
        {
            new { input = 5, output = 6, interpolation = "LINEAR" }
        };

        // Base views 0..6; anim2 adds 7..8; rotation/scale append after that (same order as WriteTwoBoneBin).
        var nextView = 7 + (anim2 ? 2 : 0);

        if (withRotation)
        {
            var timesView = nextView++;
            var quatsView = nextView++;

            var timesAcc = accessors.Count;
            accessors.Add(new
            {
                bufferView = timesView, componentType = 5126, count = 3, type = "SCALAR",
                max = new[] { 1f }, min = new[] { 0f }
            });
            var quatsAcc = accessors.Count;
            accessors.Add(new { bufferView = quatsView, componentType = 5126, count = 3, type = "VEC4" });

            clipASamplers.Add(new { input = timesAcc, output = quatsAcc, interpolation = "LINEAR" });
            clipAChannels.Add(new
            {
                sampler = clipASamplers.Count - 1,
                target = new { node = 1, path = "rotation" }
            });
        }

        if (withScale)
        {
            var timesView = nextView++;
            var scalesView = nextView;

            var timesAcc = accessors.Count;
            accessors.Add(new
            {
                bufferView = timesView, componentType = 5126, count = 2, type = "SCALAR",
                max = new[] { 1f }, min = new[] { 0f }
            });
            var scalesAcc = accessors.Count;
            accessors.Add(new { bufferView = scalesView, componentType = 5126, count = 2, type = "VEC3" });

            clipASamplers.Add(new { input = timesAcc, output = scalesAcc, interpolation = "LINEAR" });
            clipAChannels.Add(new
            {
                sampler = clipASamplers.Count - 1,
                target = new { node = 1, path = "scale" }
            });
        }

        var animations = new List<object>
        {
            new { name = "ClipA", channels = clipAChannels.ToArray(), samplers = clipASamplers.ToArray() }
        };

        if (anim2)
        {
            animations.Add(new
            {
                name = "ClipB",
                channels = new[]
                {
                    new { sampler = 0, target = new { node = 1, path = "translation" } }
                },
                samplers = new[]
                {
                    new { input = 7, output = 8, interpolation = "LINEAR" }
                }
            });
        }

        var doc = new Dictionary<string, object?>
        {
            ["asset"] = new { version = "2.0" },
            ["scenes"] = new[] { new { nodes = new[] { 0 } } },
            ["nodes"] = new object[]
            {
                new { name = "Root", children = new[] { 1, 2 } },
                new { name = "Bone0", translation = new[] { 0f, 0f, 0f }, children = new[] { 3 } },
                new { name = "MeshNode", mesh = 0, skin = 0 },
                new { name = "Bone1", translation = new[] { 0f, 1f, 0f } }
            },
            ["meshes"] = new[]
            {
                new
                {
                    name = "SkinnedTri",
                    primitives = new[]
                    {
                        new
                        {
                            attributes = new Dictionary<string, int>
                            {
                                ["POSITION"] = 0,
                                ["JOINTS_0"] = 1,
                                ["WEIGHTS_0"] = 2
                            },
                            indices = 3
                        }
                    }
                }
            },
            ["skins"] = new[]
            {
                new { name = "Armature", joints = new[] { 1, 3 }, inverseBindMatrices = 4 }
            },
            ["animations"] = animations,
            ["buffers"] = new[] { new { uri = $"{stem}.bin", byteLength = binLen } },
            ["bufferViews"] = bufferViews,
            ["accessors"] = accessors
        };

        File.WriteAllText(gltfPath, JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
        return gltfPath;
    }

    /// <summary>
    /// Skin with <paramref name="boneCount"/> joints. Assimp only emits aiBones that have
    /// weights, so each joint owns one vertex (JOINTS_0 uses ubyte — boneCount ≤ 255).
    /// </summary>
    public static string WriteManyBones(string dir, string stem, int boneCount)
    {
        if (boneCount is < 3 or > 255)
            throw new ArgumentOutOfRangeException(nameof(boneCount), "Need 3..255 verts/joints.");

        Directory.CreateDirectory(dir);
        var binPath = Path.Combine(dir, $"{stem}.bin");
        var gltfPath = Path.Combine(dir, $"{stem}.gltf");

        // Layout: POSITION (n×12) | JOINTS (n×4) | WEIGHTS (n×16) | indices | pad | IBM (n×64)
        var posLen = boneCount * 12;
        var jointsLen = boneCount * 4;
        var weightsLen = boneCount * 16;
        var indexCount = (boneCount - 2) * 3; // triangle fan from vert 0
        var indexLen = indexCount * 2;
        var afterIndices = posLen + jointsLen + weightsLen + indexLen;
        var ibmOffset = (afterIndices + 3) & ~3; // 4-byte align
        var pad = ibmOffset - afterIndices;
        var ibmLen = boneCount * 64;

        using (var stream = File.Create(binPath))
        using (var w = new BinaryWriter(stream))
        {
            for (var i = 0; i < boneCount; i++)
                WriteF3(w, i * 0.01f, 0, 0);

            for (var i = 0; i < boneCount; i++)
            {
                w.Write((byte)i);
                w.Write((byte)0);
                w.Write((byte)0);
                w.Write((byte)0);
            }

            for (var i = 0; i < boneCount; i++)
                WriteF4(w, 1f, 0f, 0f, 0f);

            for (var i = 0; i < boneCount - 2; i++)
            {
                w.Write((ushort)0);
                w.Write((ushort)(i + 1));
                w.Write((ushort)(i + 2));
            }

            for (var i = 0; i < pad; i++)
                w.Write((byte)0);

            for (var i = 0; i < boneCount; i++)
                WriteIdentityMat4(w);
        }

        var binLen = new FileInfo(binPath).Length;
        var jointsOffset = posLen;
        var weightsOffset = jointsOffset + jointsLen;
        var indicesOffset = weightsOffset + weightsLen;

        var nodes = new List<object>
        {
            new { name = "MeshRoot", mesh = 0, skin = 0, children = Enumerable.Range(1, boneCount).ToArray() }
        };
        var joints = new int[boneCount];
        for (var i = 0; i < boneCount; i++)
        {
            nodes.Add(new { name = $"Bone{i}", translation = new[] { 0f, i * 0.01f, 0f } });
            joints[i] = i + 1;
        }

        var maxX = (boneCount - 1) * 0.01f;
        var doc = new Dictionary<string, object?>
        {
            ["asset"] = new { version = "2.0" },
            ["scenes"] = new[] { new { nodes = new[] { 0 } } },
            ["nodes"] = nodes,
            ["meshes"] = new[]
            {
                new
                {
                    primitives = new[]
                    {
                        new
                        {
                            attributes = new Dictionary<string, int>
                            {
                                ["POSITION"] = 0,
                                ["JOINTS_0"] = 1,
                                ["WEIGHTS_0"] = 2
                            },
                            indices = 3
                        }
                    }
                }
            },
            ["skins"] = new[] { new { joints, inverseBindMatrices = 4 } },
            ["buffers"] = new[] { new { uri = $"{stem}.bin", byteLength = binLen } },
            ["bufferViews"] = new object[]
            {
                new { buffer = 0, byteOffset = 0, byteLength = posLen },
                new { buffer = 0, byteOffset = jointsOffset, byteLength = jointsLen },
                new { buffer = 0, byteOffset = weightsOffset, byteLength = weightsLen },
                new { buffer = 0, byteOffset = indicesOffset, byteLength = indexLen },
                new { buffer = 0, byteOffset = ibmOffset, byteLength = ibmLen }
            },
            ["accessors"] = new object[]
            {
                new
                {
                    bufferView = 0, componentType = 5126, count = boneCount, type = "VEC3",
                    max = new[] { maxX, 0f, 0f }, min = new[] { 0f, 0f, 0f }
                },
                new { bufferView = 1, componentType = 5121, count = boneCount, type = "VEC4" },
                new { bufferView = 2, componentType = 5126, count = boneCount, type = "VEC4" },
                new { bufferView = 3, componentType = 5123, count = indexCount, type = "SCALAR" },
                new { bufferView = 4, componentType = 5126, count = boneCount, type = "MAT4" }
            }
        };

        File.WriteAllText(gltfPath, JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
        return gltfPath;
    }

    /// <summary>Writes the shared two-bone bin and returns bufferView (offset, length) in write order.</summary>
    private static List<(int Offset, int Length)> WriteTwoBoneBin(
        string binPath,
        bool anim2,
        bool withRotation = false,
        bool withScale = false)
    {
        var views = new List<(int Offset, int Length)>();
        using var stream = File.Create(binPath);
        using var w = new BinaryWriter(stream);

        void Mark(int length)
        {
            var end = (int)w.BaseStream.Position;
            views.Add((end - length, length));
        }

        long Begin() => w.BaseStream.Position;

        var start = Begin();
        WriteF3(w, 0, 0, 0);
        WriteF3(w, 1, 0, 0);
        WriteF3(w, 0, 1, 0);
        Mark((int)(w.BaseStream.Position - start));

        start = Begin();
        for (var i = 0; i < 3; i++)
        {
            w.Write((byte)0);
            w.Write((byte)1);
            w.Write((byte)0);
            w.Write((byte)0);
        }
        Mark((int)(w.BaseStream.Position - start));

        start = Begin();
        for (var i = 0; i < 3; i++)
            WriteF4(w, 0.75f, 0.25f, 0f, 0f);
        Mark((int)(w.BaseStream.Position - start));

        start = Begin();
        w.Write((ushort)0);
        w.Write((ushort)1);
        w.Write((ushort)2);
        Mark((int)(w.BaseStream.Position - start)); // 6 bytes — pad ushort is outside the view
        w.Write((ushort)0);

        start = Begin();
        WriteIdentityMat4(w);
        WriteIdentityMat4(w);
        Mark((int)(w.BaseStream.Position - start));

        start = Begin();
        w.Write(0f);
        w.Write(1f);
        Mark((int)(w.BaseStream.Position - start));

        start = Begin();
        WriteF3(w, 0, 0, 0);
        WriteF3(w, 0, 0.5f, 0);
        Mark((int)(w.BaseStream.Position - start));

        if (anim2)
        {
            start = Begin();
            w.Write(0f);
            w.Write(2f);
            Mark((int)(w.BaseStream.Position - start));

            start = Begin();
            WriteF3(w, 0, 0, 0);
            WriteF3(w, 0.5f, 0, 0);
            Mark((int)(w.BaseStream.Position - start));
        }

        if (withRotation)
        {
            // 3 keys so key strides past index 0 are exercised (interop layout bugs hide on key 0).
            start = Begin();
            w.Write(0f);
            w.Write(0.5f);
            w.Write(1f);
            Mark((int)(w.BaseStream.Position - start));

            start = Begin();
            WriteF4(w, 0f, 0f, 0f, 1f);
            WriteF4(w, 0f, 0f, 0.38268343f, 0.92387953f);
            WriteF4(w, 0f, 0f, 0.70710678f, 0.70710678f);
            Mark((int)(w.BaseStream.Position - start));
        }

        if (withScale)
        {
            start = Begin();
            w.Write(0f);
            w.Write(1f);
            Mark((int)(w.BaseStream.Position - start));

            start = Begin();
            WriteF3(w, 1f, 1f, 1f);
            WriteF3(w, 2f, 2f, 2f);
            Mark((int)(w.BaseStream.Position - start));
        }

        return views;
    }

    private static void WriteF3(BinaryWriter w, float x, float y, float z)
    {
        w.Write(x);
        w.Write(y);
        w.Write(z);
    }

    private static void WriteF4(BinaryWriter w, float x, float y, float z, float ww)
    {
        w.Write(x);
        w.Write(y);
        w.Write(z);
        w.Write(ww);
    }

    private static void WriteIdentityMat4(BinaryWriter w)
    {
        for (var col = 0; col < 4; col++)
        for (var row = 0; row < 4; row++)
            w.Write(row == col ? 1f : 0f);
    }
}
