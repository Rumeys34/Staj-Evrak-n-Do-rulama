
using System.Security.Cryptography;

namespace StajEvrakDogrulama.Yardimcilar
{
    public static class SifreYardimcisi
    {
        public static string SifreyiHashle(string sifre)
        {
            byte[] tuz = RandomNumberGenerator.GetBytes(16);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                sifre,
                tuz,
                600000,
                HashAlgorithmName.SHA256,
                32
            );

            return Convert.ToBase64String(tuz) + ":" +
                   Convert.ToBase64String(hash);
        }

        public static bool SifreDogruMu(
            string girilenSifre,
            string kayitliHash)
        {
            string[] parcalar = kayitliHash.Split(':');

            if (parcalar.Length != 2)
                return false;

            byte[] tuz = Convert.FromBase64String(parcalar[0]);

            byte[] eskiHash = Convert.FromBase64String(parcalar[1]);

            byte[] yeniHash = Rfc2898DeriveBytes.Pbkdf2(
                girilenSifre,
                tuz,
                600000,
                HashAlgorithmName.SHA256,
                32
            );

            return CryptographicOperations.FixedTimeEquals(
                eskiHash,
                yeniHash
            );
        }
    }
}
