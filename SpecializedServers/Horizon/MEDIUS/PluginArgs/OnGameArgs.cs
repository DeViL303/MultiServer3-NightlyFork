using Horizon.MUM;

namespace Horizon.MEDIUS.PluginArgs
{
    public class OnGameArgs
    {
        /// <summary>
        /// Game.
        /// </summary>
        public Game? Game { get; set; }

        public override string ToString()
        {
            return base.ToString() + " " +
                $"Game: {Game}";
        }
    }
}