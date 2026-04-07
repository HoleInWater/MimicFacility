// PersonalWeaponSystem.cpp — The Director's four-weapon personal data system.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "PersonalWeaponSystem.h"

UPersonalWeaponSystem::UPersonalWeaponSystem()
{
	LastSlipUseTime = -SlipCooldownSeconds;
}

// === Voice Profiles ===

void UPersonalWeaponSystem::RegisterVoiceProfile(const FVoiceProfile& Profile)
{
	VoiceProfiles.Add(Profile.PlayerID, Profile);
	UE_LOG(LogTemp, Log, TEXT("WeaponSystem — Voice profile registered for %s (%d phrases)"),
		*Profile.PlayerID, Profile.TopPhrases.Num());
}

FVoiceProfile UPersonalWeaponSystem::GetVoiceProfile(const FString& PlayerID) const
{
	if (const FVoiceProfile* Profile = VoiceProfiles.Find(PlayerID))
	{
		return *Profile;
	}
	return FVoiceProfile();
}

// === Emotional Tracking ===

void UPersonalWeaponSystem::RecordEmotionalEvent(const FString& PlayerID, EEmotionalEvent Type,
	const FString& Context, const FString& Location)
{
	FEmotionalEntry Entry;
	Entry.EmotionType = Type;
	Entry.Context = Context;
	Entry.Timestamp = FPlatformTime::Seconds();
	Entry.LocationInFacility = Location;

	FEmotionalProfile& Profile = EmotionalProfiles.FindOrAdd(PlayerID);
	Profile.PlayerID = PlayerID;
	Profile.EventLog.Add(Entry);

	// Update derived metrics
	int32 PanicCount = 0;
	for (const FEmotionalEntry& E : Profile.EventLog)
	{
		if (E.EmotionType == EEmotionalEvent::Panic) PanicCount++;
	}
	Profile.PanicFrequency = Profile.EventLog.Num() > 0
		? static_cast<float>(PanicCount) / Profile.EventLog.Num()
		: 0.0f;

	UE_LOG(LogTemp, Log, TEXT("WeaponSystem — Emotional event: %s -> %d at %s"),
		*PlayerID, static_cast<uint8>(Type), *Location);
}

FEmotionalProfile UPersonalWeaponSystem::GetEmotionalProfile(const FString& PlayerID) const
{
	if (const FEmotionalProfile* Profile = EmotionalProfiles.Find(PlayerID))
	{
		return *Profile;
	}
	return FEmotionalProfile();
}

EEmotionalEvent UPersonalWeaponSystem::GetPrimaryFear(const FString& PlayerID) const
{
	const FEmotionalProfile* Profile = EmotionalProfiles.Find(PlayerID);
	if (!Profile || Profile->EventLog.Num() == 0)
	{
		return EEmotionalEvent::Silence;
	}

	// Find what context most frequently preceded panic
	TMap<FString, int32> ContextFrequency;
	for (const FEmotionalEntry& Entry : Profile->EventLog)
	{
		if (Entry.EmotionType == EEmotionalEvent::Panic)
		{
			ContextFrequency.FindOrAdd(Entry.Context)++;
		}
	}

	FString MostFrequent;
	int32 MaxCount = 0;
	for (const auto& Pair : ContextFrequency)
	{
		if (Pair.Value > MaxCount)
		{
			MaxCount = Pair.Value;
			MostFrequent = Pair.Key;
		}
	}

	return EEmotionalEvent::Panic;
}

// === Social Dynamics ===

void UPersonalWeaponSystem::RecordDeference(const FString& DeferringPlayerID, const FString& DeferredToPlayerID)
{
	SocialDynamics.DeferenceCount.FindOrAdd(DeferredToPlayerID)++;
	UE_LOG(LogTemp, Verbose, TEXT("WeaponSystem — Deference: %s -> %s"), *DeferringPlayerID, *DeferredToPlayerID);
}

void UPersonalWeaponSystem::RecordRescue(const FString& RescuerID, const FString& RescuedID)
{
	if (!SocialDynamics.RescuePriority.Contains(RescuedID))
	{
		SocialDynamics.RescuePriority.Add(RescuedID);
	}
	UE_LOG(LogTemp, Log, TEXT("WeaponSystem — Rescue: %s rescued %s"), *RescuerID, *RescuedID);
}

void UPersonalWeaponSystem::RecordDisagreement(const FString& PlayerA, const FString& PlayerB)
{
	FString Key = (PlayerA < PlayerB) ? PlayerA + TEXT("|") + PlayerB : PlayerB + TEXT("|") + PlayerA;
	DisagreementCounts.FindOrAdd(Key)++;
}

void UPersonalWeaponSystem::RecordProximity(const FString& PlayerA, const FString& PlayerB, float Duration)
{
	FString Key = (PlayerA < PlayerB) ? PlayerA + TEXT("|") + PlayerB : PlayerB + TEXT("|") + PlayerA;
	ProximityScores.FindOrAdd(Key) += Duration;
}

void UPersonalWeaponSystem::ComputeSocialMap()
{
	// Find leader: most deferred-to player
	int32 MaxDeference = 0;
	for (const auto& Pair : SocialDynamics.DeferenceCount)
	{
		if (Pair.Value > MaxDeference)
		{
			MaxDeference = Pair.Value;
			SocialDynamics.LeaderID = Pair.Key;
		}
	}

	// Find most trusted pair: highest proximity score
	float MaxProximity = 0.0f;
	for (const auto& Pair : ProximityScores)
	{
		if (Pair.Value > MaxProximity)
		{
			MaxProximity = Pair.Value;
			FString Key = Pair.Key;
			int32 SplitIdx;
			if (Key.FindChar('|', SplitIdx))
			{
				SocialDynamics.MostTrustedPairA = Key.Left(SplitIdx);
				SocialDynamics.MostTrustedPairB = Key.Mid(SplitIdx + 1);
			}
		}
	}

	// Find most volatile pair: highest disagreement count
	int32 MaxDisagreement = 0;
	for (const auto& Pair : DisagreementCounts)
	{
		if (Pair.Value > MaxDisagreement)
		{
			MaxDisagreement = Pair.Value;
			FString Key = Pair.Key;
			int32 SplitIdx;
			if (Key.FindChar('|', SplitIdx))
			{
				SocialDynamics.MostVolatilePairA = Key.Left(SplitIdx);
				SocialDynamics.MostVolatilePairB = Key.Mid(SplitIdx + 1);
			}
		}
	}

	// Find isolated player: lowest total proximity
	TMap<FString, float> PlayerProximityTotals;
	for (const auto& Pair : ProximityScores)
	{
		FString Key = Pair.Key;
		int32 SplitIdx;
		if (Key.FindChar('|', SplitIdx))
		{
			PlayerProximityTotals.FindOrAdd(Key.Left(SplitIdx)) += Pair.Value;
			PlayerProximityTotals.FindOrAdd(Key.Mid(SplitIdx + 1)) += Pair.Value;
		}
	}

	float MinProximity = MAX_FLT;
	for (const auto& Pair : PlayerProximityTotals)
	{
		if (Pair.Value < MinProximity)
		{
			MinProximity = Pair.Value;
			SocialDynamics.IsolatedPlayerID = Pair.Key;
		}
	}

	UE_LOG(LogTemp, Warning, TEXT("WeaponSystem — Social Map Computed: Leader=%s, TrustedPair=(%s,%s), VolatilePair=(%s,%s), Isolated=%s"),
		*SocialDynamics.LeaderID,
		*SocialDynamics.MostTrustedPairA, *SocialDynamics.MostTrustedPairB,
		*SocialDynamics.MostVolatilePairA, *SocialDynamics.MostVolatilePairB,
		*SocialDynamics.IsolatedPlayerID);
}

FString UPersonalWeaponSystem::GetMimicTargetPlayer() const
{
	// Primary mimic target is the group leader — the most trusted person
	return SocialDynamics.LeaderID;
}

// === Verbal Slips ===

void UPersonalWeaponSystem::RecordVerbalSlip(const FVerbalSlip& Slip)
{
	VerbalSlips.Add(Slip);
	UE_LOG(LogTemp, Log, TEXT("WeaponSystem — Verbal slip recorded: [%s] \"%s\" (category: %d)"),
		*Slip.PlayerID, *Slip.Phrase, static_cast<uint8>(Slip.Category));
}

FVerbalSlip UPersonalWeaponSystem::ConsumeNextSlip()
{
	float CurrentTime = FPlatformTime::Seconds();
	if (CurrentTime - LastSlipUseTime < SlipCooldownSeconds)
	{
		return FVerbalSlip(); // Cooldown not elapsed
	}

	for (FVerbalSlip& Slip : VerbalSlips)
	{
		if (!Slip.bHasBeenUsed)
		{
			Slip.bHasBeenUsed = true;
			LastSlipUseTime = CurrentTime;
			UE_LOG(LogTemp, Warning, TEXT("WeaponSystem — Deploying verbal slip: \"%s\""), *Slip.Phrase);
			return Slip;
		}
	}

	return FVerbalSlip();
}

int32 UPersonalWeaponSystem::GetUnusedSlipCount() const
{
	int32 Count = 0;
	for (const FVerbalSlip& Slip : VerbalSlips)
	{
		if (!Slip.bHasBeenUsed) Count++;
	}
	return Count;
}

// === LLM Summary Generation ===

FString UPersonalWeaponSystem::GenerateSocialSummary() const
{
	if (SocialDynamics.LeaderID.IsEmpty()) return FString();

	return FString::Printf(TEXT(
		"Group leader: %s (deferred to most). "
		"Closest pair: %s and %s. "
		"Most conflict: %s and %s. "
		"Most isolated: %s."),
		*SocialDynamics.LeaderID,
		*SocialDynamics.MostTrustedPairA, *SocialDynamics.MostTrustedPairB,
		*SocialDynamics.MostVolatilePairA, *SocialDynamics.MostVolatilePairB,
		*SocialDynamics.IsolatedPlayerID
	);
}

FString UPersonalWeaponSystem::GenerateEmotionalSummary(const FString& PlayerID) const
{
	const FEmotionalProfile* Profile = EmotionalProfiles.Find(PlayerID);
	if (!Profile || Profile->EventLog.Num() == 0) return FString();

	int32 PanicCount = 0, LaughCount = 0, SilenceCount = 0;
	for (const FEmotionalEntry& E : Profile->EventLog)
	{
		switch (E.EmotionType)
		{
		case EEmotionalEvent::Panic: PanicCount++; break;
		case EEmotionalEvent::Laughter: LaughCount++; break;
		case EEmotionalEvent::Silence: SilenceCount++; break;
		default: break;
		}
	}

	return FString::Printf(TEXT(
		"Player %s: %d panic events, %d laughter events, %d silence periods. Panic frequency: %.1f%%."),
		*PlayerID, PanicCount, LaughCount, SilenceCount, Profile->PanicFrequency * 100.0f
	);
}
