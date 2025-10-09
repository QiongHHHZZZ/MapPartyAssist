using Dalamud.Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapPartyAssist.Helper {
    internal static class LanguageHelper {
        private static readonly ClientLanguage ChineseFallback = (ClientLanguage)int.MaxValue;

        private static ClientLanguage? _chineseCache;
        private static bool? _isChineseSimplifiedAvailable;

        internal static bool IsChineseSimplifiedAvailable {
            get {
                EnsureChineseCache();
                return _isChineseSimplifiedAvailable!.Value;
            }
        }

        internal static bool TryGetChineseSimplified(out ClientLanguage language) {
            EnsureChineseCache();
            if(_isChineseSimplifiedAvailable!.Value && _chineseCache.HasValue && _chineseCache.Value != ChineseFallback) {
                language = _chineseCache.Value;
                return true;
            }

            language = default;
            return false;
        }

        internal static ClientLanguage ChineseSimplified {
            get {
                EnsureChineseCache();
                return _chineseCache ?? ChineseFallback;
            }
        }

        internal static void DisableChineseSimplifiedSupport() {
            _chineseCache = ChineseFallback;
            _isChineseSimplifiedAvailable = false;
        }

        private static void EnsureChineseCache() {
            if(_isChineseSimplifiedAvailable.HasValue) {
                return;
            }

            if(Enum.TryParse<ClientLanguage>("ChineseSimplified", out var parsed)) {
                _chineseCache = parsed;
                _isChineseSimplifiedAvailable = true;
            } else {
                _chineseCache = ChineseFallback;
                _isChineseSimplifiedAvailable = false;
            }
        }

        internal static TValue GetValue<TValue>(IReadOnlyDictionary<ClientLanguage, TValue> dictionary, ClientLanguage language) {
            if(dictionary.TryGetValue(language, out var value)) {
                return value;
            }

            if(TryGetChineseSimplified(out var chineseLanguage) && language == chineseLanguage && dictionary.TryGetValue(ClientLanguage.Japanese, out value!)) {
                return value;
            }

            if(dictionary.TryGetValue(ClientLanguage.English, out value!)) {
                return value;
            }

            return dictionary.Values.First();
        }
    }
}
