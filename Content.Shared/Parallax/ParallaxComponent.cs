using JetBrains.Annotations;
using Robust.Shared.GameStates;

namespace Content.Shared.Parallax;

/// <summary>
/// Handles per-map parallax
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ParallaxComponent : Component
{
    // I wish I could use a typeserializer here but parallax is extremely client-dependent.
    [DataField, AutoNetworkedField]
    public string Parallax = "Default";
    // hullrot edit
    //for smooth change between old and new parallax
    [DataField, AutoNetworkedField]
    public string? SwappedParallax;

    [AutoNetworkedField]
    public float SwapDuration; //in seconds

    [AutoNetworkedField]
    public float SwapTimer;
    // Hullrot edit end

    public bool IsSwapping => SwappedParallax != null;

    [UsedImplicitly, ViewVariables(VVAccess.ReadWrite)]
    // ReSharper disable once InconsistentNaming
    public string ParallaxVV
    {
        get => Parallax;
        set
        {
            if (value.Equals(Parallax)) return;
            Parallax = value;
            IoCManager.Resolve<IEntityManager>().Dirty(this);
        }
    }
}
