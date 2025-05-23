using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using System.Numerics;
using Robust.Shared.Random;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Projectiles;
using Content.Server.Shuttles.Components;
using Robust.Shared.Physics.Components;


namespace Content.Server._Crescent.ProximityFuse;

public sealed class ProximityFuseSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ProximityFuseComponent, TransformComponent>(); // get all proximity fuse components
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (TryComp<ProjectileComponent>(uid, out var projectile) && TryComp<GunComponent>(projectile.Shooter, out var shooterGunComp) && TryComp<TransformComponent>(projectile.Shooter, out var shooterTransform))
            {
                float distance = float.MaxValue;
                float closestDistance = float.MaxValue;
                float closestSpeed = float.MaxValue;
                float collisionSpeedMagnitude = shooterGunComp.ProjectileSpeed;
                var shipQuery = EntityQueryEnumerator<ThrusterComponent, TransformComponent>();
                while (shipQuery.MoveNext(out var tUid, out var tComp, out var tXform)) // output the closest grid and relative velocities
                {
                    if (shooterTransform.GridUid == tXform.GridUid)
                        return;

                    if (!TryComp<PhysicsComponent>(uid, out var ourPhysics) || !TryComp<PhysicsComponent>(tXform.GridUid, out var theirPhysics))
                        return;

                    var ourVelocity = ourPhysics.LinearVelocity;
                    var velocity = theirPhysics.LinearVelocity;

                    var speedVector = Vector2.Subtract(ourVelocity, velocity);
                    collisionSpeedMagnitude = (float) Math.Abs(speedVector.Length());
                    distance = Vector2.Distance(
                        _transform.ToMapCoordinates(xform.Coordinates).Position,
                        _transform.ToMapCoordinates(tXform.Coordinates).Position
                    );
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestSpeed = collisionSpeedMagnitude;
                    }
                }
                if (comp.SafetyTime >= 0.5f)
                {
                    if (closestDistance >= comp.MaxRange)
                        comp.Fuse = comp.MaxRange / collisionSpeedMagnitude * _random.NextFloat(0.6f, 1.5f); // calculate how long it will take to get to the target then add some noise
                    else
                        comp.Fuse -= frameTime;
                    if (closestDistance <= comp.MinRange)
                        Detonate(uid);

                    if (comp.Fuse <= 0f)
                        Detonate(uid);
                }
                else
                    comp.SafetyTime += frameTime;
            }
        }
    }
    public void Detonate(EntityUid uid) // if object has an explosive component then explode it, otherwise delete the object
    {
        if (TryComp<ExplosiveComponent>(uid, out var explosiveComp))
            _entMan.System<ExplosionSystem>().TriggerExplosive(uid);
        else
            _entMan.DeleteEntity(uid);
    }
}
