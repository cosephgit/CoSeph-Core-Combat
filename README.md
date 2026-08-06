# CoSeph.Core.Combat

Cheap, exactly reproducible hit detection for 3D games whose gameplay is really 2D — plus the
firing-cadence rules that decide when a weapon may shoot at all.

Plain .NET 8, no third-party dependencies and no game engine. Positions are `System.Numerics`
vectors, time is a float delta — nothing here knows what a frame, a scene, or a physics world is.

## Status

**v0.0.2.** Being built test-first, and **every subject here is currently a stub**. `dotnet test`
reports 63 failures and 0 passes, and that is the intended state, not a broken build.

**CI is red by design.** It runs that same suite, so the badge stays red until the last contract
lands. Read a run's log instead — it names which contracts are still unmet, and that list shortens
with each commit.

Nothing is tagged yet, and nothing here should be pinned. `v1.0.0` is the first release a consuming
project can depend on, and it lands when the last contract goes green.

| Type | Purpose | Status |
| --- | --- | --- |
| `HitPlane` | The 3D→2D reduction everything else is built on. | 🔴 stub |
| `AreaHits.InBeam` | Targets inside a beam, ordered near to far. | 🔴 stub |
| `AreaHits.InCircle` | Targets inside a circle — the splash counterpart to `InBeam`. | 🔴 stub |
| `AreaHits.InRect` | Targets inside an axis-aligned rectangle — an area that is a place rather than a reach. | 🔴 stub |
| `FiringCadence` | Shot interval, burst pattern, and the aim tolerance a weapon may fire within. | 🔴 stub |

The failing tests are the specification. A contract settled by writing the implementation is a
contract nobody got to argue with — and the decisions here are exactly the arguable kind: which
boundaries are inclusive, what a frame hitch costs, whether an abandoned burst still pays its pause.
Each is implemented as its own deliberate step, once its contract is agreed.

Everything described below is therefore a **specified** contract rather than a shipped one.

## Following along

The history is the artefact. Every test was written before any implementation existed, and each
commit takes exactly one contract green, so the failure count falls a step at a time rather than in
one jump. Start at the first commit and read forwards — the status table above moves with each one,
and the commit messages carry the reasoning behind the decision that step settled.

## The idea

Many action games are 3D only in presentation. Cover is full-height, units stand on a floor, and
nothing meaningful happens above or below anything else — so a hit test that reasons in three
dimensions is paying for a dimension the design never uses.

Flatten first and hit detection becomes cheaper than a shape query, uncapped, ordered by
construction, identical on every run from the same inputs, and testable with no engine present at
all. That last property is the one that matters most: it puts hit resolution inside a seeded
simulation instead of downstream of a physics server, so a headless or training run resolves fights
the same way the game does.

**The limit, stated rather than left to be discovered:** a game with genuine verticality in its
combat — flying units, shootable ledges, cover shorter than a target — is outside what this models,
and no tuning fixes it. Use the physics server there.

## Installing

Source drop-in. Copy the `.cs` files into your project, or vendor the repo with
`git subtree`/`git submodule`. Everything is in the `CoSeph.Core.Combat` namespace.

## AreaHits

Flat shapes tested against target origins. One contract for the whole family: everything inside,
ordered nearest first.

```csharp
IReadOnlyList<Enemy> enemies = ...;   // Enemy is your own type

Vector2 from = HitPlane.Flatten(barrel, GroundPlane.Xz);

List<AreaHit<Enemy>> hits = AreaHits.InBeam(
    origin: from, direction: facing, length: 12f, halfWidth: 0.25f,
    candidates: enemies,
    positionOf: e => HitPlane.Flatten(e.Position, GroundPlane.Xz));

foreach (AreaHit<Enemy> hit in hits)
    hit.Target.Damage(damage);   // hit.Distance is how far along the beam it stood
```

**`T` is unconstrained.** `positionOf` is the only route from a candidate to a coordinate, so
`e.Position` is `Enemy`'s own member rather than one this package asks for — a field, a method, or a
lookup keyed by id all serve. An entity id or a bare array index works as a candidate for the same
reason.

Deciding where a beam *stops* is the caller's job — that is the half a physics engine is genuinely
better at, and keeping it out here is what leaves selection a pure function.

Worth knowing:

- **Both boundaries are inclusive.** A target exactly at `length` is hit, and one exactly
  `halfWidth` off the axis is hit. A beam stopped by a wall still damages what stands against it.
- **A zero-size area hits nothing**, including a target standing on the origin. That covers a zero
  `halfWidth` as well as a zero `length` — a beam with no width has no more area than one with no
  length, so a target dead on its axis is not hit either.
- **An invalid area throws** `ArgumentOutOfRangeException`: a negative `length`, `halfWidth` or
  `radius`, a zero-length `direction`, or an inside-out rectangle. Zero size is a defined answer, but
  these are not answers at all, and quietly selecting nothing for them would hide the mistake behind a
  beam that never connects — the hardest kind of aiming bug to trace back to its cause.
- **Targets are points.** A target's own radius is not modelled, so an area is no larger against a
  big target than a small one. Size is the area's property alone.
- **Nothing is occluded.** A target behind a wall but inside the area is inside the area, and comes
  back as a hit. Cover is the caller's — see below.
- **Nothing bounds the hit count.** If your weapons have limited penetration, truncate the result.
- **`direction` need not be unit length** — it is normalised internally.
- **Ordering is total, not just sorted.** Targets at equal distance come back in candidate order
  every time, so a seeded run reproduces itself exactly.

### Rectangles

`InRect` is the shape that is a *place* rather than a reach — a room, a zone, a blast bay. It takes
two opposite corners rather than a corner and a size, and it measures from the rectangle's **centre**,
so "nearest first" means nearest to the middle of the area and not to whichever corner it was handed.
Beyond the family rules above:

- **Both axes at once.** Inside on one and outside on the other is outside. An "or" here would take in
  the whole cross through the rectangle.
- **A degenerate rectangle hits nothing**, the same rule as the beam's zero `halfWidth`: a rect flat on
  one axis is a line, and a line has no area to be inside of.
- **Grid conventions are the caller's.** Code holding a half-open grid rect owns the conversion to
  inclusive corners; this knows only about the flat plane it is given.

### Cover and line of sight

Cover is the caller's: these functions see geometry and nothing else, and knowing about walls would
mean holding your world. Sight composes from outside in either direction:

- **Trace first, then test.** Cast line of sight to the target point — one cheap ray, and one you
  likely want anyway — then run the hit test out to the length that trace established rather than the
  weapon's nominal one. The area stops where sight stops.
- **Or narrow the candidates.** Build the list from what already has line of sight: a visibility set,
  a room or region index, whatever your game already queries to decide who is shootable. What never
  enters cannot be hit, and the ordering contract is untouched by the filtering.

Neither is more correct — pick whichever your game can already answer cheaply.

## HitPlane

Which pair of axes gameplay happens on, stated once. `Xz` for a Y-up engine, `Xy` for Z-up, `Yz` for
the unusual case. Doing this by hand at each call site is how a transposed position gets loose — and
a transposed position is a bug that presents as bad aim, a long way from its cause.

`Flatten` drops the up axis, `HeightOf` keeps it, and `Restore` puts a flat result back at a chosen
height for drawing. All three throw `ArgumentOutOfRangeException` on a plane outside the enum rather
than silently picking one — `Restore` in its own right, since the drawing path reaches it without
necessarily having passed through `Flatten` first.

## FiringCadence

Whether a weapon may fire on a given tick. It holds no notion of what it is shooting at: drive it
with a delta and whether a target is present, and query your own world on the ticks it reports an
application.

```csharp
var cadence = new FiringCadence(fireInterval: 0.5f, burstSize: 3, burstDelay: 2f);

CadenceStep step = cadence.Update(delta, targetPresent: target != null);
if (step.Lit)      StartSustainedEffects();
if (step.Applies)  ApplyDamage();
if (step.Released) StopSustainedEffects();
```

Worth knowing:

- **`burstSize` at or below zero means no burst** — the weapon fires continuously at the interval,
  and `burstDelay` is ignored.
- **`burstDelay` replaces the inter-shot gap rather than adding to it.** The pause after a burst is
  `max(fireInterval, burstDelay)`, so a delay shorter than the interval changes nothing and a delay
  of zero is indistinguishable from no burst *in its firing pattern* — though with a non-zero
  `burstSize` it still raises a `Lit`/`Released` pair per burst, so the two differ to anything
  watching the edges rather than the shots.
- **`Lit` and `Released` bound a burst, not an engagement.** `Lit` is raised on a burst's first
  application and not again until the next one; `Released` when that burst completes, or when the
  target is lost. A burst bounds a lit beam's duration the way a magazine bounds a bullet turret's,
  so the burst delay is a genuine dark gap. With `burstSize` at or below zero there is no burst to
  complete: the weapon lights once and only losing the target releases it.
- **`fireInterval` must be positive.** The constructor throws `ArgumentOutOfRangeException`
  otherwise, since an interval of zero would apply on every tick — a mistyped interval silently
  becoming a weapon that fires at the tick rate.
- **A long delta applies once, never twice.** Time beyond one interval is discarded rather than
  banked, so a frame hitch costs a shot instead of producing a catch-up burst. Firing fractionally
  slow is the safer failure than double damage.
- **Losing the target abandons the burst without charging the delay.** Not shooting is already the
  pause the delay exists to create; charging it again on re-acquisition would penalise the weapon
  twice for a target that stepped behind cover.
- **`Release` is idempotent**, returning whether that call was the one that stopped the weapon — so
  routes that can race each other (target lost, weapon sold, weapon disabled) raise one release
  between them rather than one each.

### The aim tolerance

A weapon that may only fire at an exact lock will strobe when it holds a sustained beam on a moving
target: every tick spent tracking releases and re-lights it. `ToleranceFor` gives the aim error a
weapon is still permitted to *fire* at — the half-angle its own width subtends at the target's
distance — and `PermitsFire` tests a residual against it. Pass a tolerance of `0` for an impulse
weapon and the gate is an exact lock, unchanged.

**This gates firing, not hitting.** A permitted shot is one worth taking, not one promised to land.
Everything between the trigger and the target is your game's business and applies after this says
yes: scatter, projectile travel against a target that keeps moving, an obstacle that arrives
mid-flight, a to-hit roll. The half-angle is derived from the weapon's own width because that is the
cheapest honest estimate of *close enough to be worth a shot* — it is not a claim about where the
shot ends up, and the weapon's width should be calibrated to its accuracy.

## Tests

xUnit, no engine and no environment variables:

```
dotnet test Tests/CoSeph.Core.Combat.Tests.csproj
```

The test project compiles `../*.cs` directly rather than referencing a built assembly.

Every rule listed above has a named test asserting it. The boundary cases — a target exactly at the
length, a delta spanning two intervals, a burst abandoned mid-way — are there because none of them
are catchable by eye in a running game. Test values are chosen to be exact in binary floating point,
so a failure means the logic rather than the test's own arithmetic.
