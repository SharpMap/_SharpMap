using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Editors.Snapping;

namespace SharpMap.Editors.Interactors.Network
{
    /// <summary>
    /// HACK: Interface to add support for moving features without auto-snapping to possible other branch.
    /// Needed because StructureInteractor and BranchFeatureInteractor both need this functionality but do not share a base class.
    /// Hence this interface that is implemented by both. 
    /// 
    /// Improve snapping in MapControl editors to make it work without hack
    /// </summary>
    public interface IBranchMaintainableInteractor
    {
        void Stop(SnapResult snapResult, bool stayOnSameBranch);
    }
}