using DelftTools.Functions;
using DelftTools.Shell.Gui.Swf;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Gui.Swf
{
    [TestFixture]
    public class SelectFunctionViewTest
    {
        [Test]
        [Category("Windows.Forms")]
        public void Show()
        {
            var view = new SelectFunctionView();
            view.Data = new SelectFunctionViewData
                            {
                                CurrentFunction = new Function() {Name = "Aap"},
                                FunctionType = typeof (Function),
                                SelectableFunctions =
                                    new[]
                                        {
                                            new Function {Name = "Aap"}, new Function {Name = "Noot"},
                                            new Function {Name = "Mies"}
                                        }
                            };
            WindowsFormsTestHelper.ShowModal(view);
        }
    }
}