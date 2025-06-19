using SharpGLTF.Schema2;

namespace Engine
{
    class ModelLoader
    {
        public static void LoadGlbModel(string path, out float[] vertices, out uint[] indices)
        {
            var model = ModelRoot.Load(path);

            var positionsList = new List<float>();
            var indicesList = new List<uint>();

            uint vertexOffset = 0;

            foreach (var node in model.DefaultScene.VisualChildren)
            {
                TraverseNode(node, ref vertexOffset, positionsList, indicesList);
            }

            vertices = positionsList.ToArray();
            indices = indicesList.ToArray();
        }

        private static void TraverseNode(Node node, ref uint vertexOffset, List<float> vertices, List<uint> indices)
        {
            if (node.Mesh != null)
            {
                foreach (var primitive in node.Mesh.Primitives)
                {
                    var posAccessor = primitive.GetVertexAccessor("POSITION");
                    var indexAccessor = primitive.IndexAccessor;

                    var positions = posAccessor.AsVector3Array();
                    var localIndices = indexAccessor.AsIndicesArray();

                    foreach (var v in positions)
                    {
                        vertices.Add(v.X );
                        vertices.Add(v.Y);
                        vertices.Add(v.Z);
                    }

                    foreach (var i in localIndices)
                    {
                        indices.Add(vertexOffset + (uint)i);
                    }

                    vertexOffset += (uint)positions.Count;
                }
            }

            foreach (var child in node.VisualChildren)
            {
                TraverseNode(child, ref vertexOffset, vertices, indices);
            }
        }
    }
}