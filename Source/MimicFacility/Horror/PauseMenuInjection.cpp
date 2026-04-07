// PauseMenuInjection.cpp — Selects recent player phrases for eerie pause menu overlay.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "PauseMenuInjection.h"

UPauseMenuInjection::UPauseMenuInjection()
{
	bInjectionPending = false;
}

void UPauseMenuInjection::FeedPlayerPhrase(const FString& Phrase)
{
	if (Phrase.IsEmpty()) return;

	RecentPhrases.Add(Phrase);
	if (RecentPhrases.Num() > MaxStoredPhrases)
	{
		RecentPhrases.RemoveAt(0);
	}
}

void UPauseMenuInjection::InjectIntoPauseMenu()
{
	if (RecentPhrases.Num() == 0) return;

	PendingPhrases.Empty();
	TArray<int32> UsedIndices;

	int32 Count = FMath::Min(DisplayCount, RecentPhrases.Num());
	while (PendingPhrases.Num() < Count)
	{
		int32 Index = FMath::RandRange(0, RecentPhrases.Num() - 1);
		if (!UsedIndices.Contains(Index))
		{
			UsedIndices.Add(Index);
			PendingPhrases.Add(RecentPhrases[Index]);
		}
	}

	bInjectionPending = true;
}

bool UPauseMenuInjection::ConsumePhrases(TArray<FString>& OutPhrases) const
{
	if (!bInjectionPending) return false;

	OutPhrases = PendingPhrases;
	bInjectionPending = false;
	return true;
}
