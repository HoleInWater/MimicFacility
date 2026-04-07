// VoiceLearningSubsystem.cpp — Voice Learning Subsystem with debug test methods.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "VoiceLearningSubsystem.h"

void UVoiceLearningSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	UE_LOG(LogTemp, Log, TEXT("VoiceLearningSubsystem initialized."));
}

void UVoiceLearningSubsystem::Deinitialize()
{
	ClearAllData();
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

void UVoiceLearningSubsystem::DebugAddPhrase(const FString& PlayerID, const FString& PhraseText, const TArray<FString>& Witnesses)
{
	FVoicePhrase NewPhrase;
	NewPhrase.Text = PhraseText;
	NewPhrase.SessionTimestamp = FPlatformTime::Seconds();
	NewPhrase.WitnessPlayerIDs = Witnesses;

	PhraseDatabase.FindOrAdd(PlayerID).Add(NewPhrase);
	UE_LOG(LogTemp, Log, TEXT("VoiceLearn — Added phrase for %s: \"%s\" (witnesses: %d)"), *PlayerID, *PhraseText, Witnesses.Num());
}

void UVoiceLearningSubsystem::DebugSetTriggerWords(const FString& PlayerID, const TArray<FString>& Words)
{
	TriggerWordAssignments.Add(PlayerID, Words);
	UE_LOG(LogTemp, Log, TEXT("VoiceLearn — Set %d trigger words for %s"), Words.Num(), *PlayerID);
	for (const FString& Word : Words)
	{
		UE_LOG(LogTemp, Log, TEXT("  Trigger: \"%s\""), *Word);
	}
}

void UVoiceLearningSubsystem::SelectTriggerWordsForAllPlayers()
{
	for (auto& Pair : PhraseDatabase)
	{
		const FString& PlayerID = Pair.Key;
		const TArray<FVoicePhrase>& Phrases = Pair.Value;

		// Count word frequency across all phrases for this player
		TMap<FString, int32> WordCounts;
		for (const FVoicePhrase& Phrase : Phrases)
		{
			TArray<FString> Words;
			Phrase.Text.ParseIntoArrayWS(Words);
			for (const FString& Word : Words)
			{
				FString Lower = Word.ToLower();
				// Skip very short words
				if (Lower.Len() >= 3)
				{
					WordCounts.FindOrAdd(Lower)++;
				}
			}
		}

		// Pick top 3-5 most frequent words as triggers
		WordCounts.ValueSort([](int32 A, int32 B) { return A > B; });

		TArray<FString> SelectedTriggers;
		int32 Count = 0;
		for (auto& WordPair : WordCounts)
		{
			if (Count >= 5) break;
			SelectedTriggers.Add(WordPair.Key);
			Count++;
		}

		if (SelectedTriggers.Num() > 0)
		{
			TriggerWordAssignments.Add(PlayerID, SelectedTriggers);
			UE_LOG(LogTemp, Warning, TEXT("VoiceLearn — Trigger words for %s: %s"), *PlayerID, *FString::Join(SelectedTriggers, TEXT(", ")));
		}
	}
}

bool UVoiceLearningSubsystem::CheckTriggerWord(const FString& PlayerID, const FString& SpokenWord) const
{
	if (const TArray<FString>* Words = TriggerWordAssignments.Find(PlayerID))
	{
		FString Lower = SpokenWord.ToLower();
		for (const FString& Trigger : *Words)
		{
			if (Lower.Contains(Trigger))
			{
				UE_LOG(LogTemp, Warning, TEXT("TRIGGER WORD DETECTED! Player: %s Word: \"%s\""), *PlayerID, *Trigger);
				return true;
			}
		}
	}
	return false;
}

int32 UVoiceLearningSubsystem::GetPhraseCount(const FString& PlayerID) const
{
	if (const TArray<FVoicePhrase>* Phrases = PhraseDatabase.Find(PlayerID))
	{
		return Phrases->Num();
	}
	return 0;
}

void UVoiceLearningSubsystem::ClearAllData()
{
	PhraseDatabase.Empty();
	TriggerWordAssignments.Empty();
	UE_LOG(LogTemp, Log, TEXT("VoiceLearningSubsystem — All data cleared."));
}
