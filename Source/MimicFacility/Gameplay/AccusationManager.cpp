// AccusationManager.cpp — Three-phase accusation protocol implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "AccusationManager.h"
#include "Characters/MimicBase.h"
#include "Kismet/GameplayStatics.h"

AAccusationManager::AAccusationManager()
{
	PrimaryActorTick.bCanEverTick = true;
	bReplicates = true;
	CurrentPhase = EAccusationPhase::Idle;
	DeliberationDuration = 15.0f;
	VotingDuration = 10.0f;
	PhaseTimer = 0.0f;
	ExpectedVoterCount = 4;
}

void AAccusationManager::BeginPlay()
{
	Super::BeginPlay();
}

void AAccusationManager::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	if (!HasAuthority() || CurrentPhase == EAccusationPhase::Idle)
		return;

	PhaseTimer -= DeltaTime;

	if (PhaseTimer <= 0.0f)
	{
		switch (CurrentPhase)
		{
		case EAccusationPhase::Deliberation:
			BeginVoting();
			break;
		case EAccusationPhase::Voting:
			ResolveAccusation();
			break;
		case EAccusationPhase::Resolving:
			CurrentPhase = EAccusationPhase::Idle;
			break;
		default:
			break;
		}
	}
}

bool AAccusationManager::InitiateAccusation(const FString& AccuserID, const FString& AccusedID)
{
	if (CurrentPhase != EAccusationPhase::Idle)
	{
		UE_LOG(LogTemp, Warning, TEXT("AccusationManager — Cannot accuse while another accusation is active"));
		return false;
	}

	ActiveAccusation = FAccusationRecord();
	ActiveAccusation.AccuserID = AccuserID;
	ActiveAccusation.AccusedID = AccusedID;
	ActiveAccusation.Timestamp = GetWorld()->GetTimeSeconds();

	UE_LOG(LogTemp, Warning, TEXT("=== ACCUSATION: %s accuses %s ==="), *AccuserID, *AccusedID);

	OnAccusationStarted.Broadcast(AccuserID, AccusedID);
	BeginDeliberation();
	return true;
}

void AAccusationManager::BeginDeliberation()
{
	CurrentPhase = EAccusationPhase::Deliberation;
	PhaseTimer = DeliberationDuration;
	UE_LOG(LogTemp, Log, TEXT("AccusationManager — Deliberation phase: %.0f seconds"), DeliberationDuration);
}

void AAccusationManager::BeginVoting()
{
	CurrentPhase = EAccusationPhase::Voting;
	PhaseTimer = VotingDuration;
	UE_LOG(LogTemp, Log, TEXT("AccusationManager — Voting phase: %.0f seconds"), VotingDuration);
}

void AAccusationManager::CastVote(const FString& VoterID, EAccusationVote Vote)
{
	if (CurrentPhase != EAccusationPhase::Voting)
	{
		UE_LOG(LogTemp, Warning, TEXT("AccusationManager — Cannot vote outside voting phase"));
		return;
	}

	ActiveAccusation.Votes.Add(VoterID, Vote);
	UE_LOG(LogTemp, Log, TEXT("AccusationManager — %s voted %s"),
		*VoterID, Vote == EAccusationVote::Contain ? TEXT("CONTAIN") : TEXT("RELEASE"));

	// Auto-resolve if all votes are in
	if (ActiveAccusation.Votes.Num() >= ExpectedVoterCount)
	{
		PhaseTimer = 0.0f;
	}
}

bool AAccusationManager::CheckIfAccusedIsMimic(const FString& AccusedID) const
{
	// Check if the accused player ID corresponds to a Mimic actor
	TArray<AActor*> Mimics;
	UGameplayStatics::GetAllActorsOfClass(GetWorld(), AMimicBase::StaticClass(), Mimics);

	for (AActor* Actor : Mimics)
	{
		AMimicBase* Mimic = Cast<AMimicBase>(Actor);
		if (Mimic && Mimic->GetName().Contains(AccusedID))
		{
			return true;
		}
	}
	return false;
}

EAccusationVote AAccusationManager::GetDirectorTiebreaker() const
{
	// Director breaks ties — at high corruption, it may vote against truth
	// This is a simplified version; full implementation reads CorruptionTracker
	return EAccusationVote::Contain;
}

void AAccusationManager::ResolveAccusation()
{
	CurrentPhase = EAccusationPhase::Resolving;
	PhaseTimer = 2.0f;

	// Tally votes
	int32 ContainVotes = 0;
	int32 ReleaseVotes = 0;

	for (const auto& VotePair : ActiveAccusation.Votes)
	{
		if (VotePair.Value == EAccusationVote::Contain) ContainVotes++;
		else if (VotePair.Value == EAccusationVote::Release) ReleaseVotes++;
	}

	// Fill missing votes as Release (abstention = release)
	int32 MissingVotes = ExpectedVoterCount - ActiveAccusation.Votes.Num();
	ReleaseVotes += MissingVotes;

	// Tie resolution
	bool bTiebroken = false;
	if (ContainVotes == ReleaseVotes)
	{
		EAccusationVote DirectorVote = GetDirectorTiebreaker();
		if (DirectorVote == EAccusationVote::Contain) ContainVotes++;
		else ReleaseVotes++;
		bTiebroken = true;
		UE_LOG(LogTemp, Warning, TEXT("AccusationManager — TIE. Director breaks it: %s"),
			DirectorVote == EAccusationVote::Contain ? TEXT("CONTAIN") : TEXT("RELEASE"));
	}

	bool bContain = ContainVotes > ReleaseVotes;
	ActiveAccusation.bAccusedWasMimic = CheckIfAccusedIsMimic(ActiveAccusation.AccusedID);

	if (bContain && ActiveAccusation.bAccusedWasMimic)
	{
		ActiveAccusation.Result = bTiebroken ? EAccusationResult::TieBrokenByDirector : EAccusationResult::MimicContained;
		UE_LOG(LogTemp, Warning, TEXT("=== RESULT: MIMIC CONTAINED (%d-%d) ==="), ContainVotes, ReleaseVotes);
	}
	else if (bContain && !ActiveAccusation.bAccusedWasMimic)
	{
		ActiveAccusation.Result = EAccusationResult::FalsePositive;
		UE_LOG(LogTemp, Error, TEXT("=== RESULT: FALSE POSITIVE — Real player contained! (%d-%d) ==="), ContainVotes, ReleaseVotes);
	}
	else if (!bContain && ActiveAccusation.bAccusedWasMimic)
	{
		ActiveAccusation.Result = EAccusationResult::MimicReleased;
		UE_LOG(LogTemp, Warning, TEXT("=== RESULT: MIMIC RELEASED (%d-%d) ==="), ContainVotes, ReleaseVotes);
	}
	else
	{
		ActiveAccusation.Result = EAccusationResult::RealReleased;
		UE_LOG(LogTemp, Log, TEXT("=== RESULT: Real player released (%d-%d) ==="), ContainVotes, ReleaseVotes);
	}

	AccusationHistory.Add(ActiveAccusation);
	OnAccusationResolved.Broadcast(ActiveAccusation);
}

int32 AAccusationManager::GetFalsePositiveCount() const
{
	int32 Count = 0;
	for (const FAccusationRecord& Record : AccusationHistory)
	{
		if (Record.Result == EAccusationResult::FalsePositive) Count++;
	}
	return Count;
}
