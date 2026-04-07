// SporeFilter.cpp — Spore Filter: reduces hallucination effects from environmental spore clouds.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "SporeFilter.h"

ASporeFilter::ASporeFilter()
{
	GearName = FText::FromString(TEXT("Spore Filter"));
	bIsConsumable = false;
	FilterEfficiency = 0.75f;
	FilterDuration = 120.0f;
	bIsWorn = false;
	WearTime = 0.0f;
}

void ASporeFilter::Activate()
{
	bIsWorn = !bIsWorn;
	UE_LOG(LogTemp, Log, TEXT("SporeFilter %s (efficiency: %.0f%%, duration: %.0fs)"),
		bIsWorn ? TEXT("EQUIPPED") : TEXT("REMOVED"), FilterEfficiency * 100.0f, FilterDuration);
}
