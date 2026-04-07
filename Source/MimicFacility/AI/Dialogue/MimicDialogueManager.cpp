// MimicDialogueManager.cpp — Dialogue coordination with cooldown management and impersonation target selection.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicDialogueManager.h"
#include "Characters/MimicBase.h"
#include "AI/LLM/PromptBuilder.h"
#include "AI/VoiceLearningSubsystem.h"
#include "Components/AudioComponent.h"
#include "Engine/World.h"
#include "GameFramework/PlayerController.h"
#include "Kismet/GameplayStatics.h"
#include "Net/UnrealNetwork.h"
#include "TimerManager.h"

AMimicDialogueManager::AMimicDialogueManager()
{
	PrimaryActorTick.bCanEverTick = false;
	bReplicates = true;
	bDialogueInProgress = false;
	LastGlobalDialogueTime = -GlobalCooldown;
}

void AMimicDialogueManager::BeginPlay()
{
	Super::BeginPlay();

	if (HasAuthority())
	{
		PromptBuilder = NewObject<UPromptBuilder>(this);
		GetWorldTimerManager().SetTimer(EvaluationTimerHandle, this,
			&AMimicDialogueManager::EvaluateDialogueOpportunities, EvaluationInterval, true);
	}
}

void AMimicDialogueManager::RegisterMimic(AMimicBase* Mimic)
{
	if (Mimic && !ActiveMimics.Contains(Mimic))
	{
		ActiveMimics.Add(Mimic);
		UE_LOG(LogTemp, Log, TEXT("DialogueManager: Registered mimic %s"), *Mimic->GetName());
	}
}

void AMimicDialogueManager::UnregisterMimic(AMimicBase* Mimic)
{
	ActiveMimics.Remove(Mimic);
}

void AMimicDialogueManager::EvaluateDialogueOpportunities()
{
	const float CurrentTime = GetWorld()->GetTimeSeconds();

	if (CurrentTime - LastGlobalDialogueTime < GlobalCooldown)
	{
		return;
	}

	for (AMimicBase* Mimic : ActiveMimics)
	{
		if (!IsValid(Mimic) || Mimic->GetMimicState() == EMimicState::Aggressive)
		{
			continue;
		}

		const int32 MimicID = Mimic->GetUniqueID();
		if (PerMimicLastDialogueTime.Contains(MimicID) &&
			CurrentTime - PerMimicLastDialogueTime[MimicID] < PerMimicCooldown)
		{
			continue;
		}

		bool bPlayerNearby = false;
		bool bPlayerLooking = false;

		for (FConstPlayerControllerIterator It = GetWorld()->GetPlayerControllerIterator(); It; ++It)
		{
			APlayerController* PC = It->Get();
			if (!PC || !PC->GetPawn())
			{
				continue;
			}

			const float Distance = FVector::Dist(Mimic->GetActorLocation(), PC->GetPawn()->GetActorLocation());
			if (Distance <= ProximityThreshold)
			{
				bPlayerNearby = true;
				if (IsPlayerLookingAtMimic(PC, Mimic))
				{
					bPlayerLooking = true;
				}
			}
		}

		if (bPlayerNearby && !bPlayerLooking)
		{
			FString TargetID = SelectImpersonationTarget(Mimic);
			if (!TargetID.IsEmpty())
			{
				FMimicDialogueEntry Entry;
				Entry.MimicID = MimicID;
				Entry.TargetPlayerID = TargetID;
				Entry.Priority = bPlayerLooking ? 0.2f : 1.0f;
				Entry.Timestamp = CurrentTime;
				RequestDialogue(Entry);
			}
		}
	}
}

void AMimicDialogueManager::RequestDialogue(const FMimicDialogueEntry& Entry)
{
	DialogueQueue.Add(Entry);
	DialogueQueue.Sort([](const FMimicDialogueEntry& A, const FMimicDialogueEntry& B)
	{
		return A.Priority > B.Priority;
	});

	if (!bDialogueInProgress)
	{
		ProcessNextDialogue();
	}
}

void AMimicDialogueManager::ProcessNextDialogue()
{
	if (DialogueQueue.Num() == 0)
	{
		bDialogueInProgress = false;
		return;
	}

	bDialogueInProgress = true;
	FMimicDialogueEntry Entry = DialogueQueue[0];
	DialogueQueue.RemoveAt(0);

	const float CurrentTime = GetWorld()->GetTimeSeconds();
	LastGlobalDialogueTime = CurrentTime;
	PerMimicLastDialogueTime.Add(Entry.MimicID, CurrentTime);

	AMimicBase* SpeakingMimic = nullptr;
	for (AMimicBase* Mimic : ActiveMimics)
	{
		if (IsValid(Mimic) && Mimic->GetUniqueID() == Entry.MimicID)
		{
			SpeakingMimic = Mimic;
			break;
		}
	}

	if (!SpeakingMimic)
	{
		bDialogueInProgress = false;
		return;
	}

	UVoiceLearningSubsystem* VoiceSubsystem = GetGameInstance()->GetSubsystem<UVoiceLearningSubsystem>();
	TArray<FVoicePhrase> Phrases = VoiceSubsystem->GetPhrasesForPlayer(Entry.TargetPlayerID);

	FString PhraseList;
	for (const FVoicePhrase& Phrase : Phrases)
	{
		PhraseList += Phrase.Text + TEXT("\n");
	}

	FMimicContext Context;
	Context.TargetPlayerName = Entry.TargetPlayerID;
	Context.PhraseList = PhraseList;
	Context.SituationContext = TEXT("Impersonating player to lure others.");

	FLLMRequest Request = PromptBuilder->BuildMimicRequest(Context);

	UE_LOG(LogTemp, Log, TEXT("DialogueManager: Mimic %d speaking as %s"),
		Entry.MimicID, *Entry.TargetPlayerID);

	SpeakingMimic->SetVoiceProfile(Entry.TargetPlayerID);

	bDialogueInProgress = false;
}

FString AMimicDialogueManager::SelectImpersonationTarget(AMimicBase* Mimic) const
{
	UVoiceLearningSubsystem* VoiceSubsystem = GetGameInstance()->GetSubsystem<UVoiceLearningSubsystem>();
	if (!VoiceSubsystem)
	{
		return FString();
	}

	FString BestTarget;
	float BestScore = -1.0f;

	for (FConstPlayerControllerIterator It = GetWorld()->GetPlayerControllerIterator(); It; ++It)
	{
		APlayerController* PC = It->Get();
		if (!PC || !PC->GetPawn())
		{
			continue;
		}

		FString PlayerID = PC->GetPawn()->GetName();
		if (VoiceSubsystem->GetPhraseCount(PlayerID) == 0)
		{
			continue;
		}

		const float Distance = FVector::Dist(Mimic->GetActorLocation(), PC->GetPawn()->GetActorLocation());
		const float DistanceScore = FMath::Clamp(1.0f - (Distance / ProximityThreshold), 0.0f, 1.0f);

		TArray<FVoicePhrase> Phrases = VoiceSubsystem->GetPhrasesForPlayer(PlayerID);
		float RecencyScore = 0.0f;
		if (Phrases.Num() > 0)
		{
			const float MostRecent = Phrases.Last().SessionTimestamp;
			const float TimeSinceSpoke = GetWorld()->GetTimeSeconds() - MostRecent;
			RecencyScore = FMath::Clamp(1.0f - (TimeSinceSpoke / 60.0f), 0.0f, 1.0f);
		}

		const float Score = (DistanceScore * 0.4f) + (RecencyScore * 0.6f);
		if (Score > BestScore)
		{
			BestScore = Score;
			BestTarget = PlayerID;
		}
	}

	return BestTarget;
}

bool AMimicDialogueManager::IsPlayerLookingAtMimic(APlayerController* PC, AMimicBase* Mimic) const
{
	if (!PC || !PC->GetPawn())
	{
		return false;
	}

	FVector CameraLocation;
	FRotator CameraRotation;
	PC->GetPlayerViewPoint(CameraLocation, CameraRotation);

	const FVector ToMimic = (Mimic->GetActorLocation() - CameraLocation).GetSafeNormal();
	const FVector CameraForward = CameraRotation.Vector();
	const float DotProduct = FVector::DotProduct(CameraForward, ToMimic);

	return DotProduct > 0.7f;
}

void AMimicDialogueManager::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);
	DOREPLIFETIME(AMimicDialogueManager, bDialogueInProgress);
}
