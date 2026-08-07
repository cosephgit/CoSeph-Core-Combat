using System;

namespace CoSeph.Core.Combat
{
    /// <summary>
    /// What one tick of a <see cref="FiringCadence"/> resolved to.
    /// </summary>
    public readonly struct CadenceStep
    {
        /// <summary>A shot, or one damage application of a sustained weapon, lands this tick.</summary>
        public bool Applies { get; }
        /// <summary>
        /// A burst began this tick - raised on its first application and not again until the next
        /// burst. With no burst configured it is raised once, when the weapon starts firing.
        /// </summary>
        public bool Lit { get; }
        /// <summary>
        /// The weapon stopped firing this tick: a burst completed, or the target was lost.
        /// </summary>
        public bool Released { get; }

        public CadenceStep(bool applies, bool lit, bool released)
        {
            Applies = applies;
            Lit = lit;
            Released = released;
        }
    }

    /// <summary>
    /// When a weapon is allowed to fire: the interval between shots, the burst pattern around them,
    /// and the aim tolerance a shot has to fall inside.
    ///
    /// Holds no notion of what it is shooting at. A caller drives it with a delta and whether it
    /// currently has a target, and queries its own world at the ticks this reports an application.
    /// </summary>
    public sealed class FiringCadence
    {
        private readonly float _fireInterval;
        /// <summary>Time since the last application, or since the weapon last had a target.</summary>
        private float _sinceApplication;

        /// <param name="burstSize">Shots per burst. At or below zero the weapon fires continuously.</param>
        /// <param name="burstDelay">The pause after a completed burst. Replaces the inter-shot gap
        /// rather than adding to it, so a delay shorter than <paramref name="fireInterval"/> has no effect.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="fireInterval"/> is zero or negative, which would apply on every tick.
        /// </exception>
        public FiringCadence(float fireInterval, int burstSize, float burstDelay)
        {
            if (fireInterval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(fireInterval), fireInterval,
                    "A weapon with no interval between its shots fires at the tick rate.");

            _fireInterval = fireInterval;
        }

        /// <summary>The weapon is mid-fire: a sustained beam is up, or a burst is under way.</summary>
        public bool IsLit => throw new NotImplementedException();

        /// <summary>
        /// Advances the cadence by one tick and reports whether damage applies on it.
        ///
        /// A delta spanning more than one interval applies once, never twice - time beyond one gap
        /// is discarded rather than banked. Losing the target abandons any burst in progress without
        /// charging its delay.
        /// </summary>
        public CadenceStep Update(float delta, bool targetPresent)
        {
            if (!targetPresent)
            {
                // a weapon with nothing to shoot at is not counting down to a shot either. Holding the
                // clock at zero is what makes re-acquisition cost the same first interval a newly built
                // weapon pays, rather than letting time spent dark buy a shot the moment a target walks in
                _sinceApplication = 0f;
                return new CadenceStep(false, false, false);
            }

            _sinceApplication += delta;

            if (_sinceApplication < _fireInterval)
                return new CadenceStep(false, false, false);

            // reset rather than subtract the interval: a long delta applies once and the rest of it is
            // discarded, so a frame hitch costs a shot instead of banking a catch-up one
            _sinceApplication = 0f;
            return new CadenceStep(true, false, false);
        }

        /// <summary>
        /// Stops the weapon firing - sold, disabled, or otherwise taken out of action. Returns
        /// whether this call was the one that stopped it, so the routes that can race each other
        /// raise one release between them rather than one each.
        /// </summary>
        public bool Release()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The aim error a weapon of this half-width can still connect at, for a target at this
        /// distance: the half-angle its own damage volume subtends there.
        /// </summary>
        public static float ToleranceFor(float halfWidth, float distance)
        {
            // the half-angle of the right triangle whose opposite side is the weapon's half-width and
            // whose adjacent side is the range. Atan2 rather than Atan of a ratio so a target standing
            // on the weapon divides by nothing and answers a quarter turn, which is the honest reading
            // of it: at no distance at all, no aim is far enough off to miss by
            return MathF.Atan2(halfWidth, distance);
        }

        /// <summary>
        /// Whether an aim that is off by <paramref name="residualAngle"/> radians may fire. A
        /// tolerance of zero permits only an exact lock, which is how an impulse weapon behaves.
        /// </summary>
        public static bool PermitsFire(float residualAngle, float tolerance)
        {
            // the residual's sign is which way the aim is off, never how far, so it is discarded
            // before the comparison. Inclusive, matching the hit tests: a target exactly on an area's
            // edge is hit, so an aim exactly on the tolerance may take the shot that hits it
            return MathF.Abs(residualAngle) <= tolerance;
        }
    }
}
