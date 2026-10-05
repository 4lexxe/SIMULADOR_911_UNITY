using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EmergencyDispatch
{
    // Explicit text rehearsal mode.
    public static class OfflineConversation
    {
        public static string Normalize(string text)
        {
            var result = new StringBuilder();
            foreach (char c in (text ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) result.Append(c);
            return result.ToString().Normalize(NormalizationForm.FormC);
        }
        private static bool Contains(string text, params string[] terms) => Array.Exists(terms, text.Contains);
        public static string Respond(CallSession session, string text, int turn)
        {
            string query = Normalize(text);
            var replies = new List<string>();

            // Realistic overwhelmed reaction if operator asks 3+ facts at once
            int askedCount = 0;
            foreach (var fact in session.Definition.facts)
            {
                if (Array.Exists(fact.keywords, query.Contains)) askedCount++;
            }
            if (askedCount >= 3)
            {
                session.RegisterTag(turn, "confusing");
                session.AdjustEmotion(turn, -4);
                return "¡Pará, por favor! Me estás preguntando muchas cosas juntas y no puedo pensar con los nervios... Preguntame de a una por favor.";
            }

            bool verify = Contains(query, "lo viste", "viste el", "como sabes", "como lo sabes", "lo observaste", "estas segura", "estas seguro", "supon", "confirmar");

            foreach (var fact in session.Definition.facts)
            {
                bool verifying = fact.uncertain && verify;
                if (!verifying && !Array.Exists(fact.keywords, query.Contains)) continue;

                bool repeated = session.Facts.TryGetValue(fact.id, out var old) && (old.verified || !verifying);
                if (repeated) session.RegisterTag(turn, "repeated");
                else session.RegisterTag(turn, "relevant");
                session.RegisterFact(turn, "offline-" + turn + "-" + fact.id, fact.id, verifying ? fact.verifiedValue : fact.value, !fact.uncertain || verifying);
                replies.Add(verifying ? fact.verificationResponse : fact.response);
                if (verifying) session.RegisterTag(turn, "confirmation");
            }

            if (Contains(query, "tranquil", "calma", "entiendo", "te escucho", "estoy escuchando", "debe ser", "estoy aca", "estoy con vos", "respira"))
            {
                session.RegisterTag(turn, "active_listening"); session.AdjustEmotion(turn, 5);
                replies.Add(session.Calm > 40 ? "Gracias por la calma. Decime qué necesitás saber." : "Está bien... respiro hondo, pero te escucho.");
            }
            if (session.Facts.Count > 0 && Contains(query, "confirmo", "entendi", "entonces", "es correcto", "repasar"))
            {
                session.RegisterTag(turn, "confirmation"); replies.Add("Sí, eso es exactamente lo que te conté.");
            }
            if (session.DispatchedUnit != null && Contains(query, "en camino", "asigne", "enviamos", "despache", "va a llegar"))
            {
                session.RegisterTag(turn, "closure"); replies.Add("Gracias por avisarme. Entendí que la ayuda ya está en camino.");
            }
            if (replies.Count > 0 && text.Length < 240) session.RegisterTag(turn, "clear");
            if (replies.Count == 0) return "No sé cómo responderte eso con los nervios. ¿Podés hacerme una pregunta más concreta?";
            return string.Join(" ", replies);
        }
    }
}
