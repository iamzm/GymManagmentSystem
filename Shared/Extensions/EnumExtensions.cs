using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Shared.Extensions {
    public static class EnumExtensions {
        /// <summary>
        /// The Enum Member's <see cref="DisplayAttribute"/> Name, Falling Back To The Member Name.
        /// Without This, A Value Like <c>WeightLoss</c> Reaches The UI Verbatim Instead Of Reading
        /// "Weight Loss &amp; Fat Burning".
        /// </summary>
        public static string GetDisplayName(this Enum value) {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            return member?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
        }
    }
}
