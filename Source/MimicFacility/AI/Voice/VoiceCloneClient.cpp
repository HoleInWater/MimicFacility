// VoiceCloneClient.cpp — Chatterbox voice cloning sidecar HTTP client implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "VoiceCloneClient.h"
#include "HttpModule.h"
#include "Interfaces/IHttpRequest.h"
#include "Interfaces/IHttpResponse.h"
#include "Serialization/JsonSerializer.h"
#include "Dom/JsonObject.h"
#include "Dom/JsonValue.h"

UVoiceCloneClient::UVoiceCloneClient()
{
	ServerHost = TEXT("http://127.0.0.1");
	ServerPort = 8100;
	bServerAvailable = false;
}

void UVoiceCloneClient::SetEndpoint(const FString& Host, int32 Port)
{
	ServerHost = Host;
	ServerPort = Port;
}

void UVoiceCloneClient::CheckServerHealth()
{
	FString URL = FString::Printf(TEXT("%s:%d/api/health"), *ServerHost, ServerPort);

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> HttpRequest = FHttpModule::Get().CreateRequest();
	HttpRequest->SetURL(URL);
	HttpRequest->SetVerb(TEXT("GET"));
	HttpRequest->SetTimeout(3.0f);
	HttpRequest->OnProcessRequestComplete().BindUObject(this, &UVoiceCloneClient::HandleHealthCheck);
	HttpRequest->ProcessRequest();
}

void UVoiceCloneClient::HandleHealthCheck(FHttpRequestPtr HttpRequest, FHttpResponsePtr HttpResponse, bool bConnectedSuccessfully)
{
	bServerAvailable = bConnectedSuccessfully && HttpResponse.IsValid() && HttpResponse->GetResponseCode() == 200;
	UE_LOG(LogTemp, Log, TEXT("VoiceCloneClient — Server %s (health check: %s)"),
		bServerAvailable ? TEXT("AVAILABLE") : TEXT("UNAVAILABLE"),
		bConnectedSuccessfully ? TEXT("connected") : TEXT("failed"));
}

FString UVoiceCloneClient::BuildRequestJSON(const FVoiceCloneRequest& Request) const
{
	TSharedPtr<FJsonObject> JsonObj = MakeShared<FJsonObject>();
	JsonObj->SetStringField(TEXT("text"), Request.Text);
	JsonObj->SetStringField(TEXT("speaker_reference_id"), Request.SpeakerReferenceID);
	JsonObj->SetNumberField(TEXT("temperature"), Request.Temperature);
	JsonObj->SetNumberField(TEXT("exaggeration_factor"), Request.ExaggerationFactor);

	FString OutputString;
	TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&OutputString);
	FJsonSerializer::Serialize(JsonObj.ToSharedRef(), Writer);
	return OutputString;
}

FVoiceCloneResponse UVoiceCloneClient::ParseResponseData(FHttpResponsePtr HttpResponse) const
{
	FVoiceCloneResponse Result;

	FString SampleRateHeader = HttpResponse->GetHeader(TEXT("X-Sample-Rate"));
	FString DurationHeader = HttpResponse->GetHeader(TEXT("X-Duration-Seconds"));

	Result.AudioData = TArray<uint8>(HttpResponse->GetContent());
	Result.SampleRate = SampleRateHeader.IsEmpty() ? 24000 : FCString::Atoi(*SampleRateHeader);
	Result.DurationSeconds = DurationHeader.IsEmpty() ? 0.0f : FCString::Atof(*DurationHeader);
	Result.bSuccess = Result.AudioData.Num() > 0;

	return Result;
}

void UVoiceCloneClient::SendCloneRequest(const FVoiceCloneRequest& Request, FOnVoiceCloneComplete OnComplete)
{
	if (!bServerAvailable)
	{
		FVoiceCloneResponse FailResponse;
		FailResponse.ErrorMessage = TEXT("Chatterbox server not available");
		OnComplete.ExecuteIfBound(FailResponse);
		return;
	}

	FString URL = FString::Printf(TEXT("%s:%d/api/synthesize"), *ServerHost, ServerPort);
	FString Body = BuildRequestJSON(Request);

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> HttpRequest = FHttpModule::Get().CreateRequest();
	HttpRequest->SetURL(URL);
	HttpRequest->SetVerb(TEXT("POST"));
	HttpRequest->SetHeader(TEXT("Content-Type"), TEXT("application/json"));
	HttpRequest->SetHeader(TEXT("Accept"), TEXT("application/octet-stream"));
	HttpRequest->SetContentAsString(Body);
	HttpRequest->SetTimeout(15.0f);

	HttpRequest->OnProcessRequestComplete().BindLambda(
		[this, OnComplete](FHttpRequestPtr Req, FHttpResponsePtr Resp, bool bSuccess)
		{
			HandleSynthesisResponse(Req, Resp, bSuccess, OnComplete);
		});

	HttpRequest->ProcessRequest();

	UE_LOG(LogTemp, Verbose, TEXT("VoiceCloneClient — Synthesis request sent to %s (speaker: %s)"),
		*URL, *Request.SpeakerReferenceID);
}

void UVoiceCloneClient::HandleSynthesisResponse(FHttpRequestPtr HttpRequest, FHttpResponsePtr HttpResponse,
	bool bConnectedSuccessfully, FOnVoiceCloneComplete OnComplete)
{
	FVoiceCloneResponse Result;

	if (!bConnectedSuccessfully || !HttpResponse.IsValid())
	{
		Result.ErrorMessage = TEXT("Connection failed");
		OnComplete.ExecuteIfBound(Result);
		UE_LOG(LogTemp, Warning, TEXT("VoiceCloneClient — Synthesis failed: connection error"));
		return;
	}

	int32 Code = HttpResponse->GetResponseCode();
	if (Code != 200)
	{
		Result.ErrorMessage = FString::Printf(TEXT("HTTP %d"), Code);
		OnComplete.ExecuteIfBound(Result);
		UE_LOG(LogTemp, Warning, TEXT("VoiceCloneClient — Synthesis failed: HTTP %d"), Code);
		return;
	}

	Result = ParseResponseData(HttpResponse);
	if (!Result.bSuccess)
	{
		Result.ErrorMessage = TEXT("Empty audio response");
	}

	UE_LOG(LogTemp, Log, TEXT("VoiceCloneClient — Synthesis complete (%.2fs, %d bytes, %d Hz)"),
		Result.DurationSeconds, Result.AudioData.Num(), Result.SampleRate);

	OnComplete.ExecuteIfBound(Result);
}
