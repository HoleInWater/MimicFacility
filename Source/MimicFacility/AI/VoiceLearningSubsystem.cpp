// VoiceLearningSubsystem.cpp — Voice Learning Subsystem implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "VoiceLearningSubsystem.h"

void UVoiceLearningSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	// TODO: Register with VoIP subsystem to receive per-player audio streams.
}

void UVoiceLearningSubsystem::Deinitialize()
{
	// Clear all session data — no voice data persists between sessions.
	PhraseDatabase.Empty();
	TriggerWordAssignments.Empty();

	Super::Deinitialize();
}

TArray<FVoicePhrase> UVoiceLearningSubsystem::GetPhrasesForPlayer(const FString& PlayerID) const
{
	if (const TArray<FVoicePhrase>* Phrases = PhraseDatabase.Find(PlayerID))
	{
		return *Phrases;
	}
	return TArray<FVoicePhrase>();
}

TArray<FString> UVoiceLearningSubsystem::GetTriggerWords(const FString& PlayerID) const
{
	if (const TArray<FString>* Words = TriggerWordAssignments.Find(PlayerID))
	{
		return *Words;
	}
	return TArray<FString>();
}
