// ResearchTerminal.cpp — Interactive terminal that grants access to lore entries gated by keycard and corruption level.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "ResearchTerminal.h"
#include "Components/BoxComponent.h"
#include "Components/StaticMeshComponent.h"
#include "GameFramework/Character.h"
#include "Lore/LoreDatabase.h"
#include "Gear/GearBase.h"
#include "Net/UnrealNetwork.h"

AResearchTerminal::AResearchTerminal()
{
	PrimaryActorTick.bCanEverTick = false;
	bReplicates = true;

	TerminalMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("TerminalMesh"));
	RootComponent = TerminalMesh;

	InteractionZone = CreateDefaultSubobject<UBoxComponent>(TEXT("InteractionZone"));
	InteractionZone->SetupAttachment(RootComponent);
	InteractionZone->SetBoxExtent(FVector(80.0f, 80.0f, 100.0f));
	InteractionZone->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
	InteractionZone->SetCollisionResponseToAllChannels(ECR_Overlap);

	bRequiresKeycard = true;
	bIsUnlocked = false;
}

void AResearchTerminal::BeginPlay()
{
	Super::BeginPlay();
}

void AResearchTerminal::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);
	DOREPLIFETIME(AResearchTerminal, bIsUnlocked);
}

void AResearchTerminal::OnInteract(AActor* Interactor)
{
	if (!Interactor)
	{
		return;
	}

	if (bRequiresKeycard && !bIsUnlocked)
	{
		if (!HasKeycard(Interactor))
		{
			UE_LOG(LogTemp, Log, TEXT("Terminal [%s] requires keycard — access denied"), *TerminalID.ToString());
			return;
		}
		bIsUnlocked = true;
	}

	ULoreDatabase* LoreDB = GetGameInstance()->GetSubsystem<ULoreDatabase>();
	if (!LoreDB)
	{
		return;
	}

	// Corruption level 0 as default; gameplay systems should pass the real value via GetAvailableEntries
	TArray<FTerminalEntry> Entries = GetAvailableEntries(0);
	for (const FTerminalEntry& Entry : Entries)
	{
		OnEntryRead.Broadcast(Entry);
	}

	UE_LOG(LogTemp, Log, TEXT("Terminal [%s] accessed — %d entries available"), *TerminalID.ToString(), Entries.Num());
}

TArray<FTerminalEntry> AResearchTerminal::GetAvailableEntries(int32 CorruptionLevel) const
{
	TArray<FTerminalEntry> Results;

	ULoreDatabase* LoreDB = GetGameInstance()->GetSubsystem<ULoreDatabase>();
	if (!LoreDB)
	{
		return Results;
	}

	TArray<FLoreEntry> LoreEntries = LoreDB->GetEntriesForTerminal(TerminalID, CorruptionLevel);
	for (const FLoreEntry& Lore : LoreEntries)
	{
		FTerminalEntry TermEntry;
		TermEntry.Title = Lore.Title;
		TermEntry.Content = (Lore.bIsRedacted && CorruptionLevel >= Lore.MinCorruptionToReveal)
			? Lore.RedactedContent
			: Lore.Content;
		TermEntry.Author = Lore.Author;
		TermEntry.Classification = Lore.Classification;
		Results.Add(TermEntry);
	}

	return Results;
}

bool AResearchTerminal::HasKeycard(AActor* Interactor) const
{
	ACharacter* Character = Cast<ACharacter>(Interactor);
	if (!Character)
	{
		return false;
	}

	// Check attached gear actors for a keycard by name
	TArray<AActor*> AttachedActors;
	Character->GetAttachedActors(AttachedActors);
	for (AActor* Attached : AttachedActors)
	{
		AGearBase* Gear = Cast<AGearBase>(Attached);
		if (Gear && Gear->GetGearName().ToString().Contains(TEXT("Keycard")))
		{
			return true;
		}
	}

	return false;
}

void AResearchTerminal::OnRep_IsUnlocked()
{
	UE_LOG(LogTemp, Log, TEXT("Terminal [%s] unlock state replicated: %s"),
		*TerminalID.ToString(), bIsUnlocked ? TEXT("UNLOCKED") : TEXT("LOCKED"));
}
