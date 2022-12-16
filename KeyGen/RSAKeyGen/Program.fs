open System.IO;
open System.Security.Cryptography;

let rsaKey = RSA.Create();
let privateKey = rsaKey.ExportRSAPrivateKey();

File.WriteAllBytes("myRsaKey", privateKey);