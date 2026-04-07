// ResearchTerminal.h — Interactive terminal that grants access to lore entries gated by keycard and corruption level.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "ResearchTerminal.generated.h"

class UBoxComponent;
class ULoreDatabase;
struct FLoreEntry;

USTRUCT(BlueprintType)
struct FTerminalEntry
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Title;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Content;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Author;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Classification;
};

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnEntryRead, const FTerminalEntry&, Entry);

UCLASS()
class MIMICFACILITY_API AResearchTerminal : public AActor
{
	GENERATED_BODY()

public:
	AResearchTerminal();

	UFUNCTION(BlueprintCallable, Category = "Facility|Terminal")
	void OnInteract(AActor* Interactor);

	UFUNCTION(BlueprintCallable, Category = "Facility|Terminal")
	TArray<FTerminalEntry> GetAvailableEntries(int32 CorruptionLevel) const;

	UPROPERTY(BlueprintAssignable, Category = "Facility|Terminal")
	FOnEntryRead OnEntryRead;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility|Terminal")
	FName TerminalID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility")
	FName ZoneTag;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility|Terminal")
	bool bRequiresKeycard;

protected:
	virtual void BeginPlay() override;
	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Components")
	TObjectPtr<UStaticMeshComponent> TerminalMesh;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Components")
	TObjectPtr<UBoxComponent> InteractionZone;

	UPROPERTY(ReplicatedUsing = OnRep_IsUnlocked, BlueprintReadOnly, Category = "Facility|Terminal")
	bool bIsUnlocked;

private:
	UFUNCTION()
	void OnRep_IsUnlocked();

	bool HasKeycard(AActor* Interactor) const;
};
