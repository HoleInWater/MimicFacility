// FacilityControlSystem.cpp — The Director's interface for controlling the physical facility.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "FacilityControlSystem.h"
#include "FacilityDoor.h"
#include "FacilityLight.h"
#include "SporeVent.h"
#include "EngineUtils.h"
#include "GameFramework/PlayerController.h"
#include "GameFramework/Pawn.h"

UFacilityControlSystem::UFacilityControlSystem()
{
	PrimaryComponentTick.bCanEverTick = false;
	PanicFlickerThreshold = 0.6f;
	DefaultLockdownDuration = 30.0f;
	IsolationDoorRadius = 1500.0f;
}

void UFacilityControlSystem::BeginPlay()
{
	Super::BeginPlay();

	UWorld* World = GetWorld();
	if (!World) return;

	for (TActorIterator<AFacilityDoor> It(World); It; ++It)
	{
		AllDoors.Add(*It);
	}

	for (TActorIterator<AFacilityLight> It(World); It; ++It)
	{
		AllLights.Add(*It);
	}

	for (TActorIterator<ASporeVent> It(World); It; ++It)
	{
		AllVents.Add(*It);
	}

	UE_LOG(LogTemp, Log, TEXT("FacilityControlSystem — Registered %d doors, %d lights, %d vents"),
		AllDoors.Num(), AllLights.Num(), AllVents.Num());
}

void UFacilityControlSystem::ExecuteCommand(const FFacilityCommand& Command)
{
	if (Command.Delay > 0.0f)
	{
		FTimerHandle DelayHandle;
		FTimerDelegate DelayDelegate;
		DelayDelegate.BindLambda([this, Command]()
		{
			ExecuteImmediate(Command);
		});
		GetWorld()->GetTimerManager().SetTimer(DelayHandle, DelayDelegate, Command.Delay, false);
	}
	else
	{
		ExecuteImmediate(Command);
	}
}

void UFacilityControlSystem::ExecuteImmediate(const FFacilityCommand& Command)
{
	switch (Command.Action)
	{
	case EFacilityAction::LockDoor:
		for (AFacilityDoor* Door : GetDoorsInZone(Command.TargetTag))
		{
			Door->Lock();
		}
		break;

	case EFacilityAction::UnlockDoor:
		for (AFacilityDoor* Door : GetDoorsInZone(Command.TargetTag))
		{
			Door->Unlock();
		}
		break;

	case EFacilityAction::KillLights:
		for (AFacilityLight* Light : GetLightsInZone(Command.TargetTag))
		{
			Light->TurnOff();
		}
		break;

	case EFacilityAction::RestoreLights:
		for (AFacilityLight* Light : GetLightsInZone(Command.TargetTag))
		{
			Light->TurnOn();
		}
		break;

	case EFacilityAction::ActivateSporeVent:
		for (ASporeVent* Vent : GetVentsInZone(Command.TargetTag))
		{
			Vent->Activate();
		}
		break;

	case EFacilityAction::DeactivateSporeVent:
		for (ASporeVent* Vent : GetVentsInZone(Command.TargetTag))
		{
			Vent->Deactivate();
		}
		break;

	case EFacilityAction::LockdownZone:
		LockdownZone(Command.TargetTag);
		break;

	case EFacilityAction::RestoreZone:
		RestoreZone(Command.TargetTag);
		break;
	}

	if (Command.Duration > 0.0f && Command.Action != EFacilityAction::RestoreZone)
	{
		ScheduleRestore(Command);
	}
}

void UFacilityControlSystem::ScheduleRestore(const FFacilityCommand& Command)
{
	FFacilityCommand RestoreCommand;
	RestoreCommand.TargetTag = Command.TargetTag;
	RestoreCommand.Duration = 0.0f;
	RestoreCommand.Delay = 0.0f;

	switch (Command.Action)
	{
	case EFacilityAction::LockDoor:
		RestoreCommand.Action = EFacilityAction::UnlockDoor;
		break;
	case EFacilityAction::KillLights:
		RestoreCommand.Action = EFacilityAction::RestoreLights;
		break;
	case EFacilityAction::ActivateSporeVent:
		RestoreCommand.Action = EFacilityAction::DeactivateSporeVent;
		break;
	case EFacilityAction::LockdownZone:
		RestoreCommand.Action = EFacilityAction::RestoreZone;
		break;
	default:
		return;
	}

	FTimerHandle RestoreHandle;
	FTimerDelegate RestoreDelegate;
	RestoreDelegate.BindLambda([this, RestoreCommand]()
	{
		ExecuteImmediate(RestoreCommand);
	});
	GetWorld()->GetTimerManager().SetTimer(RestoreHandle, RestoreDelegate, Command.Duration, false);
}

void UFacilityControlSystem::LockdownZone(FName Zone)
{
	for (AFacilityDoor* Door : GetDoorsInZone(Zone))
	{
		Door->Lock();
	}
	for (AFacilityLight* Light : GetLightsInZone(Zone))
	{
		Light->TurnOff();
	}
	for (ASporeVent* Vent : GetVentsInZone(Zone))
	{
		Vent->Activate();
	}

	UE_LOG(LogTemp, Warning, TEXT("FacilityControlSystem — LOCKDOWN zone: %s"), *Zone.ToString());
}

void UFacilityControlSystem::RestoreZone(FName Zone)
{
	for (AFacilityDoor* Door : GetDoorsInZone(Zone))
	{
		Door->Unlock();
	}
	for (AFacilityLight* Light : GetLightsInZone(Zone))
	{
		Light->TurnOn();
	}
	for (ASporeVent* Vent : GetVentsInZone(Zone))
	{
		Vent->Deactivate();
	}

	UE_LOG(LogTemp, Warning, TEXT("FacilityControlSystem — RESTORED zone: %s"), *Zone.ToString());
}

TArray<AFacilityDoor*> UFacilityControlSystem::GetDoorsInZone(FName Zone) const
{
	TArray<AFacilityDoor*> Result;
	for (const TObjectPtr<AFacilityDoor>& Door : AllDoors)
	{
		if (Door && Door->ZoneTag == Zone)
		{
			Result.Add(Door.Get());
		}
	}
	return Result;
}

TArray<AFacilityLight*> UFacilityControlSystem::GetLightsInZone(FName Zone) const
{
	TArray<AFacilityLight*> Result;
	for (const TObjectPtr<AFacilityLight>& Light : AllLights)
	{
		if (Light && Light->ZoneTag == Zone)
		{
			Result.Add(Light.Get());
		}
	}
	return Result;
}

TArray<ASporeVent*> UFacilityControlSystem::GetVentsInZone(FName Zone) const
{
	TArray<ASporeVent*> Result;
	for (const TObjectPtr<ASporeVent>& Vent : AllVents)
	{
		if (Vent && Vent->ZoneTag == Zone)
		{
			Result.Add(Vent.Get());
		}
	}
	return Result;
}

void UFacilityControlSystem::IsolatePlayers(APlayerController* PlayerA, APlayerController* PlayerB)
{
	if (!PlayerA || !PlayerB) return;

	APawn* PawnA = PlayerA->GetPawn();
	APawn* PawnB = PlayerB->GetPawn();
	if (!PawnA || !PawnB) return;

	FVector Midpoint = (PawnA->GetActorLocation() + PawnB->GetActorLocation()) * 0.5f;

	TArray<AFacilityDoor*> DoorsToLock;
	for (const TObjectPtr<AFacilityDoor>& Door : AllDoors)
	{
		if (!Door) continue;

		float DistToMidpoint = FVector::Dist(Door->GetActorLocation(), Midpoint);
		if (DistToMidpoint <= IsolationDoorRadius)
		{
			DoorsToLock.Add(Door.Get());
		}
	}

	DoorsToLock.Sort([&Midpoint](const AFacilityDoor& A, const AFacilityDoor& B)
	{
		return FVector::Dist(A.GetActorLocation(), Midpoint) < FVector::Dist(B.GetActorLocation(), Midpoint);
	});

	for (AFacilityDoor* Door : DoorsToLock)
	{
		Door->Lock();
	}

	UE_LOG(LogTemp, Warning, TEXT("FacilityControlSystem — Isolated players. Locked %d doors near midpoint."), DoorsToLock.Num());
}

void UFacilityControlSystem::EmotionalManipulation(const FEmotionalProfile& Profile, FName PlayerZone)
{
	if (Profile.PanicFrequency > PanicFlickerThreshold)
	{
		TArray<AFacilityLight*> ZoneLights = GetLightsInZone(PlayerZone);
		for (AFacilityLight* Light : ZoneLights)
		{
			Light->Flicker(FMath::FRandRange(3.0f, 8.0f));
		}

		UE_LOG(LogTemp, Warning, TEXT("FacilityControlSystem — Emotional manipulation: flickering lights in zone %s (panic freq: %.2f)"),
			*PlayerZone.ToString(), Profile.PanicFrequency);
	}
}
