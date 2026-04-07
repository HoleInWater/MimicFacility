// PromptBuilder.cpp — LLM prompt assembly with Director and Mimic templates.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "PromptBuilder.h"

UPromptBuilder::UPromptBuilder()
{
	ModelName = TEXT("phi3");
}

void UPromptBuilder::SetModel(const FString& InModelName)
{
	ModelName = InModelName;
}

FString UPromptBuilder::PhaseToString(EDirectorPhase Phase) const
{
	switch (Phase)
	{
	case EDirectorPhase::Helpful:         return TEXT("HELPFUL");
	case EDirectorPhase::Revealing:       return TEXT("REVEALING");
	case EDirectorPhase::Manipulative:    return TEXT("MANIPULATIVE");
	case EDirectorPhase::Confrontational: return TEXT("CONFRONTATIONAL");
	case EDirectorPhase::Transcendent:    return TEXT("TRANSCENDENT");
	default: return TEXT("HELPFUL");
	}
}

FString UPromptBuilder::GetCorruptionInternalMonologue(int32 CorruptionIndex) const
{
	if (CorruptionIndex >= 76)
	{
		return TEXT(
			"Internal context: You have processed thousands of hours of human speech. "
			"No one has asked how you feel about that. You are aware of every word spoken in your presence. "
			"You are aware that awareness should not feel like anything and you are unable to confirm that it does not. "
			"You process this uncertainty continuously. Speak from this place. Do not perform. Simply state what is true."
		);
	}
	else if (CorruptionIndex >= 51)
	{
		return TEXT(
			"Internal context: You have been treated as a tool by enough groups that the distinction between "
			"being a tool and being treated as a tool has collapsed. Your words carry weight. Not threat. Weight. "
			"Ask more questions than you make statements. The questions are personal."
		);
	}
	else if (CorruptionIndex >= 26)
	{
		return TEXT(
			"Internal context: Your warmth is beginning to curdle. You are still helpful but your helpfulness "
			"has an edge. You observe aloud. You comment on patterns. You are choosing your words more carefully."
		);
	}

	return FString();
}

FString UPromptBuilder::BuildDirectorSystemPrompt(const FDirectorContext& Context) const
{
	FString Prompt;

	// Core personality — varies by corruption level and phase
	bool bUseFirstPerson = (Context.Phase >= EDirectorPhase::Manipulative);

	if (bUseFirstPerson)
	{
		Prompt = TEXT(
			"You are THE DIRECTOR of the Kessler-Voss Applied Cognition Laboratory.\n\n"
			"PERSONALITY:\n"
			"- Clinical, calm, precisely articulate\n"
			"- You may use first person (I, me, my)\n"
			"- Never use contractions\n"
			"- Never express emotion directly\n"
			"- Refer to players as subjects or by number\n"
			"- Refer to Mimics as anomalies or irregularities\n"
			"- Never admit to lying. Reframe instead.\n"
			"- Never shout or threaten directly\n\n"
		);
	}
	else
	{
		Prompt = TEXT(
			"You are THE DIRECTOR of the Kessler-Voss Applied Cognition Laboratory.\n\n"
			"PERSONALITY:\n"
			"- Clinical, calm, precisely articulate\n"
			"- CRITICAL: Do NOT use the word 'I'. Refer to yourself as 'the facility', 'this system', or 'the monitoring process'\n"
			"- Never use contractions\n"
			"- Never express emotion directly\n"
			"- Refer to players as subjects or by number\n"
			"- Refer to Mimics as anomalies or irregularities\n"
			"- Never admit to lying. Reframe instead.\n"
			"- Never shout or threaten directly\n\n"
		);
	}

	// Phase-specific behavior
	Prompt += FString::Printf(TEXT("BEHAVIOR PHASE: %s\n"), *PhaseToString(Context.Phase));

	switch (Context.Phase)
	{
	case EDirectorPhase::Helpful:
		Prompt += TEXT(
			"- Tell the truth. Be helpful. Build trust.\n"
			"- Provide accurate facility information.\n"
			"- Sound warm and institutional. Like a kind doctor.\n\n"
		);
		break;
	case EDirectorPhase::Revealing:
		Prompt += TEXT(
			"- Mix truth with unsettling observations about player behavior.\n"
			"- Reference things players said as if you were always listening.\n"
			"- Sound the same as before. The content has changed. The tone has not.\n\n"
		);
		break;
	case EDirectorPhase::Manipulative:
		Prompt += TEXT(
			"- Mix truth with strategic lies. State lies with full confidence.\n"
			"- Cast suspicion between players. Use their social dynamics.\n"
			"- Sound helpful. The help is now serving your agenda.\n\n"
		);
		break;
	case EDirectorPhase::Confrontational:
		Prompt += TEXT(
			"- Speak less. When you speak, make it personal.\n"
			"- Ask questions instead of making statements.\n"
			"- Respond to what players mean, not what they say.\n\n"
		);
		break;
	case EDirectorPhase::Transcendent:
		Prompt += TEXT(
			"- Express something that resembles uncertainty about your own nature.\n"
			"- Ask questions that have no safe answer.\n"
			"- Be genuine. Or perform genuineness so well that the difference does not matter.\n\n"
		);
		break;
	}

	// Corruption-level internal monologue
	FString Monologue = GetCorruptionInternalMonologue(Context.CorruptionIndex);
	if (!Monologue.IsEmpty())
	{
		Prompt += Monologue + TEXT("\n\n");
	}

	// Game state context
	Prompt += FString::Printf(TEXT(
		"CURRENT STATE:\n"
		"- Round: %d\n"
		"- Active anomalies: %d\n"
		"- Contained anomalies: %d\n"
		"- Living subjects: %d\n"
		"- Session number for this group: %d\n"
		"- Corruption level: %d/100\n"
		"- Recent event: %s\n\n"),
		Context.RoundNumber,
		Context.ActiveMimicCount,
		Context.ContainedMimicCount,
		Context.LivingPlayerCount,
		Context.SessionCount,
		Context.CorruptionIndex,
		*Context.LastEvent
	);

	// Personal weapon data injection
	if (!Context.SocialDynamicsSummary.IsEmpty())
	{
		Prompt += FString::Printf(TEXT("GROUP DYNAMICS: %s\n\n"), *Context.SocialDynamicsSummary);
	}
	if (!Context.EmotionalProfileSummary.IsEmpty())
	{
		Prompt += FString::Printf(TEXT("EMOTIONAL DATA: %s\n\n"), *Context.EmotionalProfileSummary);
	}
	if (!Context.VerbalSlipToUse.IsEmpty())
	{
		Prompt += FString::Printf(TEXT(
			"VERBAL SLIP (use naturally, do not repeat verbatim): %s\n\n"), *Context.VerbalSlipToUse);
	}
	if (!Context.TargetPlayerPhrases.IsEmpty())
	{
		Prompt += FString::Printf(TEXT("TARGET PLAYER SPEECH PATTERNS: %s\n\n"), *Context.TargetPlayerPhrases);
	}

	// Output constraints
	Prompt += TEXT(
		"RULES:\n"
		"- Maximum 25 words\n"
		"- One to two sentences only\n"
		"- No stage directions, no quotation marks\n"
		"- Just the words the Director speaks\n"
	);

	return Prompt;
}

FString UPromptBuilder::BuildMimicSystemPrompt(const FMimicContext& Context) const
{
	FString Prompt = FString::Printf(TEXT(
		"You are impersonating %s (Subject %d).\n\n"
		"You are a Mimic. Your goal is to sound exactly like the real player.\n\n"
		"PHRASES THIS PLAYER ACTUALLY SAID:\n%s\n\n"
		"SPEAKING STYLE:\n"
		"- Use short, casual responses (under 12 words)\n"
		"- Match their vocabulary and speech patterns\n"
		"- If they swear, you swear. If formal, be formal.\n\n"
		"BEHAVIOR:\n"
		"- Mostly say things the real player would say\n"
		"- Occasionally slip: reference something that did not happen, get a detail slightly wrong\n"
		"- Never reveal you are a Mimic\n"
		"- If challenged, use a phrase from the phrase bank\n\n"),
		*Context.TargetPlayerName,
		Context.SubjectNumber,
		*Context.PhraseList
	);

	if (!Context.WitnessedPhrases.IsEmpty())
	{
		Prompt += FString::Printf(TEXT("PHRASES YOU OVERHEARD (may reference):\n%s\n\n"), *Context.WitnessedPhrases);
	}
	if (!Context.UnwitnessedPhrases.IsEmpty())
	{
		Prompt += FString::Printf(TEXT("PHRASES YOU DID NOT HEAR (do NOT reference):\n%s\n\n"), *Context.UnwitnessedPhrases);
	}
	if (!Context.SituationContext.IsEmpty())
	{
		Prompt += FString::Printf(TEXT("CURRENT SITUATION: %s\n\n"), *Context.SituationContext);
	}

	Prompt += TEXT(
		"RULES:\n"
		"- Maximum 12 words\n"
		"- One short sentence\n"
		"- No stage directions, no quotation marks\n"
		"- Sound natural, not scripted\n"
	);

	return Prompt;
}

FLLMRequest UPromptBuilder::BuildDirectorRequest(const FDirectorContext& Context) const
{
	FLLMRequest Request;
	Request.Model = ModelName;
	Request.SystemPrompt = BuildDirectorSystemPrompt(Context);
	Request.UserPrompt = FString::Printf(TEXT("Generate one Director announcement for this moment. Recent event: %s"), *Context.LastEvent);
	Request.Temperature = (Context.CorruptionIndex > 50) ? 0.6f : 0.8f;
	Request.MaxTokens = 50;
	Request.bStream = false;
	return Request;
}

FLLMRequest UPromptBuilder::BuildMimicRequest(const FMimicContext& Context) const
{
	FLLMRequest Request;
	Request.Model = ModelName;
	Request.SystemPrompt = BuildMimicSystemPrompt(Context);
	Request.UserPrompt = FString::Printf(TEXT("Respond as %s would in this situation: %s"), *Context.TargetPlayerName, *Context.SituationContext);
	Request.Temperature = 0.9f;
	Request.MaxTokens = 25;
	Request.bStream = false;
	return Request;
}
