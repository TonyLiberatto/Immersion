namespace Neolithic
{
    class AnimProps
    {
        public string idleAnim { get; set; }
        public string actionAnim { get; set; }
        public string hasContentAnim { get; set; }

        public string[] allAnims { get => AllAnims(); }

        private string[] AllAnims()
        {
            return new string[] { idleAnim, actionAnim, hasContentAnim };
        }
    }
}