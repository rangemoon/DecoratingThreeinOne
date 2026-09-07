#if USE_UNITY_IAP
// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("2XSZSznEFbhhiyeQz+9OQDBEUVTeIdS+2AMpVe9zYNtdBAbSL57NJHvTITFaZsVLo7KnMp3uXTW6Eg01Cx8VbGpkF3ytf8DX3QYqMBIvoRJo9kUTgPJ8JGg72ehPAOcVCzOMG1vY1tnpW9jT21vY2NlRtJaPNHJlinCtCaKeAMNGN1FQnmQpQHMLALVwqp6K6Bjps0VEv4/h8F95XBn+6fG2Zl3kS1ugFU5EZq9EocYTZl5e6VvY++nU39DzX5FfLtTY2Njc2dpaAUt3NLKKCoxB3idgb3bcKEzvZ70+nuL3huUScr8c8ivgHGdrSeNs/Cy607Yau9Kqlm6O/EffEZI2WkpBSPbplFCD+HECusbtcH++BPe4RHXnmpVXqw9kGtva2NnY");
        private static int[] order = new int[] { 2,9,3,12,7,9,6,8,10,12,13,11,13,13,14 };
        private static int key = 217;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
#endif
