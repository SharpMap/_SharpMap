using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Utils;
using GeoAPI.Extensions.Feature;
using NUnit.Framework;
using SharpMap.Layers;
using SharpMap.Styles;
using SharpMap.UI.Editors;

namespace SharpMap.UI.Tests.Editors
{
    [TestFixture]
    public class FeatureEditorTest
    {
        private class TestFeatureEditor : FeatureEditor
        {
            public TestFeatureEditor(ICoordinateConverter coordinateConverter, ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject)
                : base(coordinateConverter, layer, feature, vectorStyle, editableObject)
            {
            }

            public bool IsEditable { get; set; }

            protected override bool AllowDeletionCore()
            {
                return IsEditable;
            }

            protected override bool AllowMoveCore()
            {
                return IsEditable;
            }
        }

        [Test]
        public void CanDeleteAndMoveDependsOnGroupLayerReadOnly()
        {
            var map = new Map();
            var groupLayer1 = new GroupLayer();
            var groupLayer2 = new GroupLayer();
            var layer = new VectorLayer();
            groupLayer1.Layers.Add(groupLayer2);
            groupLayer2.Layers.Add(layer);
            map.Layers.Add(groupLayer1);

            var editor = new TestFeatureEditor(null, layer, null, null, null) {IsEditable = true};

            Assert.IsTrue(editor.AllowDeletion());
            Assert.IsTrue(editor.AllowMove());

            groupLayer1.ReadOnly = true;

            Assert.IsFalse(editor.AllowDeletion());
            Assert.IsFalse(editor.AllowMove());
        }

        [Test]
        public void CanDeleteAndMoveDependsOnGroupLayerReadOnlyAndFeatureItself()
        {
            var map = new Map();
            var groupLayer1 = new GroupLayer();
            var groupLayer2 = new GroupLayer();
            var layer = new VectorLayer();
            groupLayer1.Layers.Add(groupLayer2);
            groupLayer2.Layers.Add(layer);
            map.Layers.Add(groupLayer1);

            var editor = new TestFeatureEditor(null, layer, null, null, null) { IsEditable = false };

            Assert.IsFalse(editor.AllowDeletion());
            Assert.IsFalse(editor.AllowMove());

            groupLayer1.ReadOnly = true;

            Assert.IsFalse(editor.AllowDeletion());
            Assert.IsFalse(editor.AllowMove());
        }

        [Test]
        public void CanDeleteAndMoveDoesNotCrashWithEmptyLayer()
        {
            var editor = new FeatureEditor(null, null, null, null, null) {};

            Assert.IsFalse(editor.AllowDeletion());
            Assert.IsFalse(editor.AllowMove());
        }

        [Test]
        public void CanDeleteAndMoveDoesNotCrashWithEmptyMap()
        {
            var layer = new VectorLayer();

            var editor = new FeatureEditor(null, layer, null, null, null);

            Assert.IsFalse(editor.AllowDeletion());
            Assert.IsFalse(editor.AllowMove());
        }
    }
}
