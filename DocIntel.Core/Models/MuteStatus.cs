// 

namespace DocIntel.Core.Models
{
    /// <summary>
    ///     Represents the mute state. A user can mute to elements to avoid unwanted, noisy, notifications. For example,
    ///     vulnerability bulletins might be noisy and generate annoyingly long newsletters for threat analysts.
    /// </summary>
    public class MuteStatus
    {
        public MuteStatus()
        {
            Muted = false;
        }

        /// <summary>
        ///     Whether the user muted the subscription.
        /// </summary>
        /// <value><c>True</c> if the user muted, <c>False</c> otherwise.</value>
        public bool Muted { get; set; }
    }
}