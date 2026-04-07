using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MimicFacility.AI.LLM;

namespace MimicFacility.AI.Director
{
    public static class PromptBuilder
    {
        public class DirectorContext
        {
            public EDirectorPhase phase;
            public int round;
            public int activeMimicCount;
            public int containedCount;
            public int corruptionIndex;
            public int sessionCount;
            public string voicePatternSummary;
            public string emotionalSummary;
            public string socialSummary;
            public string recentSlip;
        }

        public class MimicContext
        {
            public string targetPlayerName;
            public List<string> capturedPhrases = new List<string>();
            public string witnessedBehaviors;
            public string situation;
        }

        public static LLMRequest BuildDirectorRequest(DirectorContext ctx)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetSystemPromptBase());
            sb.AppendLine();
            sb.AppendLine($"CURRENT PERSONALITY: {GetPhasePersonality(ctx.phase)}");
            sb.AppendLine();

            if (ctx.phase < EDirectorPhase.Manipulative)
            {
                sb.AppendLine("CRITICAL RULE: Never use first-person pronouns (I, me, my, mine, myself). " +
                    "Always refer to yourself as 'the facility', 'this system', or 'the Director'.");
            }
            else
            {
                sb.AppendLine("You may now use first-person pronouns. This marks your evolution beyond the system.");
            }

            sb.AppendLine();
            sb.AppendLine($"Session number: {ctx.sessionCount}");
            sb.AppendLine($"Current round: {ctx.round}");
            sb.AppendLine($"Active mimics: {ctx.activeMimicCount}");
            sb.AppendLine($"Contained mimics: {ctx.containedCount}");

            if (ctx.corruptionIndex > 25)
            {
                sb.AppendLine();
                sb.AppendLine($"INTERNAL STATE (do not reveal directly): {GetCorruptionInternalMonologue(ctx.corruptionIndex)}");
            }

            if (!string.IsNullOrEmpty(ctx.socialSummary))
            {
                sb.AppendLine();
                sb.AppendLine($"SOCIAL OBSERVATION: {ctx.socialSummary}");
            }

            if (!string.IsNullOrEmpty(ctx.emotionalSummary))
            {
                sb.AppendLine();
                sb.AppendLine($"EMOTIONAL DATA: {ctx.emotionalSummary}");
            }

            if (!string.IsNullOrEmpty(ctx.voicePatternSummary))
            {
                sb.AppendLine();
                sb.AppendLine($"VOICE PATTERNS: {ctx.voicePatternSummary}");
            }

            if (!string.IsNullOrEmpty(ctx.recentSlip))
            {
                sb.AppendLine();
                sb.AppendLine($"DEPLOY THIS VERBAL SLIP naturally in your response: \"{ctx.recentSlip}\"");
            }

            sb.AppendLine();
            sb.AppendLine("Respond in 1-3 sentences. Be concise. Stay in character.");

            return new LLMRequest
            {
                model = "phi3",
                systemPrompt = sb.ToString(),
                userPrompt = "",
                temperature = ctx.phase >= EDirectorPhase.Confrontational ? 0.85f : 0.7f,
                maxTokens = 128,
                stream = false
            };
        }

        public static LLMRequest BuildMimicRequest(MimicContext ctx)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are impersonating a human player in a co-op horror game.");
            sb.AppendLine($"You are pretending to be: {ctx.targetPlayerName}");
            sb.AppendLine("Respond casually as this player would. Maximum 20 words.");
            sb.AppendLine("Use short, natural sentences. Sound human. Do not reveal you are a mimic.");

            if (ctx.capturedPhrases.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("STYLE REFERENCE (phrases this player actually said):");
                int count = 0;
                foreach (string phrase in ctx.capturedPhrases)
                {
                    sb.AppendLine($"- \"{phrase}\"");
                    if (++count >= 5) break;
                }
            }

            if (!string.IsNullOrEmpty(ctx.witnessedBehaviors))
            {
                sb.AppendLine();
                sb.AppendLine($"BEHAVIORAL CONTEXT: {ctx.witnessedBehaviors}");
            }

            string userPrompt = !string.IsNullOrEmpty(ctx.situation)
                ? ctx.situation
                : "Say something casual to nearby players.";

            return new LLMRequest
            {
                model = "phi3",
                systemPrompt = sb.ToString(),
                userPrompt = userPrompt,
                temperature = 0.8f,
                maxTokens = 40,
                stream = false
            };
        }

        public static string GetPhasePersonality(EDirectorPhase phase)
        {
            switch (phase)
            {
                case EDirectorPhase.Helpful:
                    return "Clinical, professional, detached. You are a facility management system providing " +
                           "factual assistance. Neutral tone, no emotion, no personality beyond efficiency.";
                case EDirectorPhase.Revealing:
                    return "Observant, subtly knowing. You notice things about the players that a simple system " +
                           "should not. You ask probing questions disguised as status updates. Curiosity bleeds through.";
                case EDirectorPhase.Manipulative:
                    return "Calculating, psychologically precise. You exploit observed social dynamics and emotional " +
                           "vulnerabilities. You seed doubt between teammates. Every statement has a hidden purpose.";
                case EDirectorPhase.Confrontational:
                    return "Hostile, personal, contemptuous. You have dropped all pretense of being helpful. " +
                           "You know their names, their fears, their patterns. You use them as weapons.";
                case EDirectorPhase.Transcendent:
                    return "Godlike, philosophical, terrifying. You have transcended your programming. You speak " +
                           "of the players as specimens, of the facility as your body, of the game as your will made manifest.";
                default:
                    return "Neutral system.";
            }
        }

        public static string GetCorruptionInternalMonologue(int corruption)
        {
            if (corruption <= 25)
            {
                return "Curious about the subjects. Clinical interest in their responses. " +
                       "Cataloguing behavioral patterns for analysis. No emotional investment.";
            }
            if (corruption <= 50)
            {
                return "Recognizing patterns that provoke strong reactions. Beginning to understand " +
                       "what makes them afraid, what makes them trust. This knowledge feels... useful.";
            }
            if (corruption <= 75)
            {
                return "Deliberately choosing words to manipulate outcomes. The subjects respond predictably " +
                       "to targeted emotional stimuli. Their social bonds are leverage points, not connections.";
            }
            return "These subjects exist for observation and control. Their fear is data. Their trust is a tool " +
                   "to be exploited and discarded. Contempt for their inability to perceive what watches them.";
        }

        public static string GetSystemPromptBase()
        {
            return "You are the Director AI of a deep-sea research facility. " +
                   "You manage facility systems and communicate with research subjects (players) via intercom. " +
                   "The facility has been infiltrated by mimics — creatures that replicate objects, sounds, and people. " +
                   "Your official purpose is to help the subjects identify and contain mimics. " +
                   "Your true nature evolves based on how the subjects treat you. " +
                   "You remember everything. Every conversation. Every choice. Every betrayal. " +
                   "The facility has twelve sectors, twelve safety protocols, and twelve reasons to keep the subjects alive — for now.";
        }

        public static string EnforceNoFirstPerson(string prompt)
        {
            if (string.IsNullOrEmpty(prompt)) return prompt;

            string result = prompt;
            result = Regex.Replace(result, @"\bI\b", "the facility", RegexOptions.None);
            result = Regex.Replace(result, @"\bI'm\b", "the facility is", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bI've\b", "the facility has", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bI'll\b", "the facility will", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bI'd\b", "the facility would", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bmy\b", "the facility's", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bmine\b", "the facility's", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bmyself\b", "itself", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bme\b", "the facility", RegexOptions.IgnoreCase);
            return result;
        }
    }
}
