// AccusationManager.h — Three-phase social deduction mechanic: Suspicion → Accusation → Judgment.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "AccusationManager.generated.h"

UENUM(BlueprintType)
enum class EAccusationPhase : uint8
{
	Idle,
	Deliberation,
	Voting,
	Resolving
};

UENUM(BlueprintType)
enum class EAccusationVote : uint8
{
	NoVote,
	Contain,
	Release
};

UENUM(BlueprintType)
enum class EAccusationResult : uint8
{
	MimicContained,
	FalsePositive,
	MimicReleased,
	RealReleased,
	TieBrokenByDirector
};

USTRUCT(BlueprintType)
struct FAccusationRecord
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly)
	FString AccuserID;

	UPROPERTY(BlueprintReadOnly)
	FString AccusedID;

	UPROPERTY(BlueprintReadOnly)
	TMap<FString, EAccusationVote> Votes;

	UPROPERTY(BlueprintReadOnly)
	EAccusationResult Result;

	UPROPERTY(BlueprintReadOnly)
	bool bAccusedWasMimic;

	UPROPERTY(BlueprintReadOnly)
	float Timestamp;

	FAccusationRecord() : Result(EAccusationResult::RealReleased), bAccusedWasMimic(false), Timestamp(0.0f) {}
};

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FOnAccusationStarted, const FString&, AccuserID, const FString&, AccusedID);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnDeliberationComplete, const FAccusationRecord&, Record);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnAccusationResolved, const FAccusationRecord&, Record);

UCLASS()
class MIMICFACILITY_API AAccusationManager : public AActor
{
	GENERATED_BODY()

public:
	AAccusationManager();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;

	UFUNCTION(BlueprintCallable, Category = "Accusation")
	bool InitiateAccusation(const FString& AccuserID, const FString& AccusedID);

	UFUNCTION(BlueprintCallable, Category = "Accusation")
	void CastVote(const FString& VoterID, EAccusationVote Vote);

	UFUNCTION(BlueprintPure, Category = "Accusation")
	EAccusationPhase GetCurrentPhase() const { return CurrentPhase; }

	UFUNCTION(BlueprintPure, Category = "Accusation")
	bool IsAccusationActive() const { return CurrentPhase != EAccusationPhase::Idle; }

	UFUNCTION(BlueprintPure, Category = "Accusation")
	TArray<FAccusationRecord> GetAccusationHistory() const { return AccusationHistory; }

	UFUNCTION(BlueprintPure, Category = "Accusation")
	int32 GetFalsePositiveCount() const;

	UPROPERTY(BlueprintAssignable)
	FOnAccusationStarted OnAccusationStarted;

	UPROPERTY(BlueprintAssignable)
	FOnDeliberationComplete OnDeliberationComplete;

	UPROPERTY(BlueprintAssignable)
	FOnAccusationResolved OnAccusationResolved;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Accusation")
	float DeliberationDuration;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Accusation")
	float VotingDuration;

private:
	void BeginDeliberation();
	void BeginVoting();
	void ResolveAccusation();
	bool CheckIfAccusedIsMimic(const FString& AccusedID) const;
	EAccusationVote GetDirectorTiebreaker() const;

	UPROPERTY()
	EAccusationPhase CurrentPhase;

	UPROPERTY()
	FAccusationRecord ActiveAccusation;

	UPROPERTY()
	TArray<FAccusationRecord> AccusationHistory;

	float PhaseTimer;
	int32 ExpectedVoterCount;
};
