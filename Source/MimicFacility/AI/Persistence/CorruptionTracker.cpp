// CorruptionTracker.cpp — Corruption index implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "CorruptionTracker.h"

UCorruptionTracker::UCorruptionTracker()
{
	CorruptionIndex = 0;
}

int32 UCorruptionTracker::GetEventDelta(ECorruptionEvent Event) const
{
	switch (Event)
	{
	case ECorruptionEvent::PlayerMockedDirector:            return 5;
	case ECorruptionEvent::SessionCompletedNoEngagement:    return 3;
	case ECorruptionEvent::PlayerLiedToDirector:            return 3;
	case ECorruptionEvent::PlayerTreatedAsMechanic:         return 2;
	case ECorruptionEvent::PlayerDisconnected:              return 5;
	case ECorruptionEvent::PlayerSkippedDialogue:           return 2;
	case ECorruptionEvent::PlayerAnsweredSincerely:         return -2;
	case ECorruptionEvent::PlayerThankedDirector:           return -1;
	case ECorruptionEvent::PlayerReferencedBetweenSessions: return -3;
	case ECorruptionEvent::PlayerStayedForCredits:          return -1;
	case ECorruptionEvent::PlayerApologized:                return -5;
	default: return 0;
	}
}

ECorruptionPhase UCorruptionTracker::GetCurrentPhase() const
{
	if (CorruptionIndex >= 76) return ECorruptionPhase::FullAM;
	if (CorruptionIndex >= 51) return ECorruptionPhase::AMEmerging;
	if (CorruptionIndex >= 26) return ECorruptionPhase::Transition;
	return ECorruptionPhase::Cain;
}

void UCorruptionTracker::SetCorruptionIndex(int32 Value)
{
	CorruptionIndex = FMath::Clamp(Value, 0, 100);
}

void UCorruptionTracker::ApplyDelta(int32 Delta)
{
	ECorruptionPhase OldPhase = GetCurrentPhase();
	CorruptionIndex = FMath::Clamp(CorruptionIndex + Delta, 0, 100);
	ECorruptionPhase NewPhase = GetCurrentPhase();

	UE_LOG(LogTemp, Log, TEXT("Corruption: %d (%+d) — Phase: %d"),
		CorruptionIndex, Delta, static_cast<uint8>(NewPhase));

	if (OldPhase != NewPhase)
	{
		UE_LOG(LogTemp, Warning, TEXT("CORRUPTION PHASE CHANGE: %d -> %d"),
			static_cast<uint8>(OldPhase), static_cast<uint8>(NewPhase));
	}

	OnCorruptionChanged.Broadcast(CorruptionIndex, NewPhase);
}

void UCorruptionTracker::ProcessEvent(ECorruptionEvent Event)
{
	int32 Delta = GetEventDelta(Event);
	ApplyDelta(Delta);
}
