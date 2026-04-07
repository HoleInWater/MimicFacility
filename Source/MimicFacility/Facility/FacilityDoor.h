// FacilityDoor.h — Lockable facility door controlled by the Director.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "FacilityDoor.generated.h"

class UBoxComponent;

UCLASS()
class MIMICFACILITY_API AFacilityDoor : public AActor
{
	GENERATED_BODY()

public:
	AFacilityDoor();

	UFUNCTION(BlueprintCallable, Category = "Facility|Door")
	void Lock();

	UFUNCTION(BlueprintCallable, Category = "Facility|Door")
	void Unlock();

	UFUNCTION(BlueprintCallable, Category = "Facility|Door")
	void ToggleOpen();

	UFUNCTION(BlueprintCallable, Category = "Facility|Door")
	void OnInteract(AActor* Interactor);

	UFUNCTION(BlueprintPure, Category = "Facility|Door")
	bool IsLocked() const { return bIsLocked; }

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility")
	FName ZoneTag;

protected:
	virtual void BeginPlay() override;
	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Components")
	TObjectPtr<UStaticMeshComponent> DoorMesh;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Components")
	TObjectPtr<UBoxComponent> BlockingVolume;

	UPROPERTY(ReplicatedUsing = OnRep_IsLocked, BlueprintReadOnly, Category = "Facility|Door")
	bool bIsLocked;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Facility|Door")
	bool bIsOpen;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility|Door")
	TObjectPtr<USoundBase> LockedSound;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility|Door")
	TObjectPtr<USoundBase> OpenSound;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility|Door")
	TObjectPtr<USoundBase> CloseSound;

private:
	UFUNCTION()
	void OnRep_IsLocked();
};
