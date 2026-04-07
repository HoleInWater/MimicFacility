// LoreDatabase.h — Persistent lore storage subsystem with corruption-gated entry reveal across three narrative channels.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "LoreDatabase.generated.h"

UENUM(BlueprintType)
enum class ELoreChannel : uint8
{
	Environmental,
	Terminal,
	Director
};

USTRUCT(BlueprintType)
struct FLoreEntry
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FName EntryID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FName TerminalID;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Title;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Content;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Author;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Classification;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	ELoreChannel Channel;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 MinCorruptionToReveal;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	bool bIsRedacted;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString RedactedContent;
};

UCLASS()
class MIMICFACILITY_API ULoreDatabase : public UGameInstanceSubsystem
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;

	UFUNCTION(BlueprintCallable, Category = "Lore")
	TArray<FLoreEntry> GetEntriesForTerminal(FName TerminalID, int32 CorruptionLevel) const;

	UFUNCTION(BlueprintCallable, Category = "Lore")
	TArray<FLoreEntry> GetEnvironmentalLore(FName ZoneTag) const;

	UFUNCTION(BlueprintCallable, Category = "Lore")
	TArray<FLoreEntry> GetDirectorLore(int32 CorruptionLevel) const;

	UFUNCTION(BlueprintCallable, Category = "Lore")
	void MarkEntryAsRead(FName EntryID);

	UFUNCTION(BlueprintPure, Category = "Lore")
	int32 GetUnreadCount() const;

	UFUNCTION(BlueprintPure, Category = "Lore")
	bool IsEntryRead(FName EntryID) const;

private:
	void LoadEntriesFromJSON();

	TMap<FName, TArray<FLoreEntry>> TerminalEntries;
	TArray<FLoreEntry> AllEntries;
	TSet<FName> ReadEntries;
};
