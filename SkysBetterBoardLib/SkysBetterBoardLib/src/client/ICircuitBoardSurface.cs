using LogicWorld.Interfaces;
using LogicWorld.Physics;

namespace SkysBetterBoardLib.Client;

public interface ICircuitBoardSurface : IComponentClientCode
{
  public bool CanMoveOn(HitInfo info) => true;
}