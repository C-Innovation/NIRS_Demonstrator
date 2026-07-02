namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class NirsCsvData
    {

        #region Protected Members

        #endregion

        #region Private Members

        #endregion

        #region Public Properties

        public string Ch740_1 {  get; set; }
        public string Ch740_2 {  get; set; }
        public string Ch740_3 {  get; set; }
        public string Ch740_4 {  get; set; }
        public string Ch850_1 {  get; set; }
        public string Ch850_2 {  get; set; }
        public string Ch850_3 {  get; set; }
        public string Ch850_4 {  get; set; }
        public string ChTotal{  get; set; }

        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public NirsCsvData( string ch740_1,
                            string ch740_2,
                            string ch740_3,
                            string ch740_4,
                            string ch850_1,
                            string ch850_2,
                            string ch850_3,
                            string ch850_4,
                            string chTotal)
        {
            Ch740_1 = ch740_1;
            Ch740_2 = ch740_2;
            Ch740_3 = ch740_3;
            Ch740_4 = ch740_4;
            Ch850_1 = ch850_1;
            Ch850_2 = ch850_2;
            Ch850_3 = ch850_3;
            Ch850_4 = ch850_4;
            ChTotal = chTotal;
        }

        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        #endregion
    }
}
