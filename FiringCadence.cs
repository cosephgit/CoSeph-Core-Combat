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
        /// <param name="burstSize">Shots per burst. At or below zero the weapon fires continuously.</param>
        /// <param name="burstDelay">The pause after a completed burst. Replaces the inter-shot gap
        /// rather than adding to it, so a delay shorter than <paramref name="fireInterval"/> has no effect.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="fireInterval"/> is zero or negative, which would apply on every tick.
        /// </exception>
        public FiringCadence(float fireInterval, int burstSize, float burstDelay)
        {
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Whether an aim that is off by <paramref name="residualAngle"/> radians may fire. A
        /// tolerance of zero permits only an exact lock, which is how an impulse weapon behaves.
        /// </summary>
        public static bool PermitsFire(float residualAngle, float tolerance)
        {
            throw new NotImplementedException();
        }
    }
}
