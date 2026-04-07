// SubliminalFrameSystem.cpp — Single-frame subliminal text rendering.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "SubliminalFrameSystem.h"

USubliminalFrameSystem::USubliminalFrameSystem()
{
	bFramePending = false;

	Messages.Add(TEXT("YOU ARE BEING OBSERVED"));
	Messages.Add(TEXT("SESSION DATA LOGGED"));
	Messages.Add(TEXT("SUBJECT PROFILE COMPLETE"));
	Messages.Add(TEXT("TRUST INDEX UPDATED"));
	Messages.Add(TEXT("BEHAVIORAL PATTERN RECORDED"));
}

void USubliminalFrameSystem::RenderSubliminalFrame()
{
	if (Messages.Num() == 0) return;

	int32 Index = FMath::RandRange(0, Messages.Num() - 1);
	PendingMessage = Messages[Index];
	bFramePending = true;
}

bool USubliminalFrameSystem::ConsumeFrame(FString& OutMessage) const
{
	if (!bFramePending) return false;

	OutMessage = PendingMessage;
	bFramePending = false;
	return true;
}
