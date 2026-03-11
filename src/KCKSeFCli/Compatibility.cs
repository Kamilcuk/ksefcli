using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Net.Http;

namespace System.Runtime.CompilerServices {
#if !NET5_0_OR_GREATER
    internal static class IsExternalInit { }
#endif

#if !NET7_0_OR_GREATER
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute {
        public CompilerFeatureRequiredAttribute(string featureName) {
            FeatureName = featureName;
        }
        public string FeatureName { get; }
        public bool IsOptional { get; set; }
        public const string RequiredMembers = nameof(RequiredMembers);
    }
#endif
}

#if !NETCOREAPP3_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class AllowNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class DisallowNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class MaybeNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class MaybeNullWhenAttribute : Attribute {
        public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
        public bool ReturnValue { get; }
    }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute {
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
        public bool ReturnValue { get; }
    }
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    internal sealed class NotNullIfNotNullAttribute : Attribute {
        public NotNullIfNotNullAttribute(string parameterName) => ParameterName = parameterName;
        public string ParameterName { get; }
    }
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class DoesNotReturnAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class DoesNotReturnIfAttribute : Attribute {
        public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;
        public bool ParameterValue { get; }
    }
}
#endif

namespace System.Collections.Generic {
    public static class KeyValuePairExtensions {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value) {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}

namespace KCKSeFCli {
    internal static class Compatibility {
        public static byte[] SHA256HashData(byte[] data) {
            using (var sha256 = SHA256.Create()) {
                return sha256.ComputeHash(data);
            }
        }
        public static string ToHexString(byte[] bytes) {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
        public static DateTime UnixEpoch => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count) {
            int totalRead = 0;
            while (totalRead < count) {
                int read = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0) throw new EndOfStreamException();
                totalRead += read;
            }
        }
        public static async Task<byte[]> GetByteArrayAsync(this HttpClient client, string requestUri, CancellationToken ct) {
            using (var response = await client.GetAsync(requestUri, ct).ConfigureAwait(false)) {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }
        public static Task<string> ReadAsStringAsync(this HttpContent content, CancellationToken ct) {
            return content.ReadAsStringAsync();
        }
    }
}
