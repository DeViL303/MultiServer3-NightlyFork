using System.IO;
using CyberBackendLibrary.HTTP;
using HttpMultipartParser;
using System.Security.Cryptography;
using System.Text;
using System;

namespace WebAPIService.NDREAMS.Aurora
{
    public static class VRSignUp
    {
        public static string? ProcessVRSignUp(byte[]? PostData, string? ContentType, string apipath)
        {
            string email = string.Empty;
            string username = string.Empty;
            string hash = string.Empty;
            string day = string.Empty;
            string? boundary = HTTPProcessor.ExtractBoundary(ContentType);

            if (!string.IsNullOrEmpty(boundary) && PostData != null)
            {
                using (MemoryStream ms = new MemoryStream(PostData))
                {
                    var data = MultipartFormDataParser.Parse(ms, boundary);

                    email = data.GetParameterValue("email");
                    username = data.GetParameterValue("username");
                    hash = data.GetParameterValue("hash");

                    ms.Flush();
                }

                byte[] SHA1Data = new byte[0];
                using (SHA1 sha1hash = SHA1.Create())
                {
                    SHA1Data = sha1hash.ComputeHash(Encoding.UTF8.GetBytes(email + "_" + username + "_" + "V305iSReuFCeRvLpt2mMh83nkeV0p9pl"));
                }
                string ExpectedHash = BitConverter.ToString(SHA1Data).Replace("-", string.Empty).ToLower();

                if (hash == ExpectedHash)
                {
                    Directory.CreateDirectory(apipath + "/NDREAMS/Aurora/VRSignUp");

                    string SignedUpProfilePath = apipath + $"/NDREAMS/Aurora/VRSignUp/{username}.txt";

                    if (File.Exists(SignedUpProfilePath))
                    {
                        string? Extractedemail = File.ReadAllText(SignedUpProfilePath).Replace("email=", string.Empty);

                        if (string.IsNullOrEmpty(Extractedemail))
                        {
                            CustomLogger.LoggerAccessor.LogWarn($"[nDreams] - VRSignUp: Profile:{SignedUpProfilePath} has an invalid format! Overwritting...");
                            File.WriteAllText(SignedUpProfilePath, $"email={email}");
                            return $"{{\"success\":\"true\",\"reward\":\"true\"}}";
                        }
                        else
                        {
                            if (Extractedemail == email)
                                return $"{{\"success\":\"true\",\"reward\":\"false\"}}";
                            else
                            {
                                File.WriteAllText(SignedUpProfilePath, $"email={email}");
                                return $"{{\"success\":\"true\",\"reward\":\"true\"}}";
                            }
                        }
                    }
                    else
                    {
                        File.WriteAllText(SignedUpProfilePath, $"email={email}");
                        return $"{{\"success\":\"true\",\"reward\":\"true\"}}";
                    }
                }
                else
                {
                    string errMsg = $"[nDreams] - VRSignUp: invalid hash sent! Received:{hash} Expected:{ExpectedHash}";
                    CustomLogger.LoggerAccessor.LogWarn(errMsg);
                    return $"{{\"success\":\"false\",\"error\":\"{errMsg}\"}}";
                }
            }

            return null;
        }
    }
}
