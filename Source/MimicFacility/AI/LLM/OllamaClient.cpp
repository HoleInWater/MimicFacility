// OllamaClient.cpp — Ollama sidecar HTTP client implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "OllamaClient.h"
#include "HttpModule.h"
#include "Interfaces/IHttpRequest.h"
#include "Interfaces/IHttpResponse.h"
#include "Serialization/JsonSerializer.h"
#include "Dom/JsonObject.h"
#include "Dom/JsonValue.h"

UOllamaClient::UOllamaClient()
{
	ServerHost = TEXT("http://127.0.0.1");
	ServerPort = 11434;
	bServerAvailable = false;
}

void UOllamaClient::SetEndpoint(const FString& Host, int32 Port)
{
	ServerHost = Host;
	ServerPort = Port;
}

void UOllamaClient::CheckServerHealth()
{
	FString URL = FString::Printf(TEXT("%s:%d/api/tags"), *ServerHost, ServerPort);

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> HttpRequest = FHttpModule::Get().CreateRequest();
	HttpRequest->SetURL(URL);
	HttpRequest->SetVerb(TEXT("GET"));
	HttpRequest->SetTimeout(3.0f);
	HttpRequest->OnProcessRequestComplete().BindUObject(this, &UOllamaClient::HandleHealthCheck);
	HttpRequest->ProcessRequest();
}

void UOllamaClient::HandleHealthCheck(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bConnectedSuccessfully)
{
	bServerAvailable = bConnectedSuccessfully && Response.IsValid() && Response->GetResponseCode() == 200;
	UE_LOG(LogTemp, Log, TEXT("OllamaClient — Server %s (health check: %s)"),
		bServerAvailable ? TEXT("AVAILABLE") : TEXT("UNAVAILABLE"),
		bConnectedSuccessfully ? TEXT("connected") : TEXT("failed"));
}

FString UOllamaClient::BuildRequestJSON(const FLLMRequest& Request) const
{
	TSharedPtr<FJsonObject> JsonObj = MakeShared<FJsonObject>();
	JsonObj->SetStringField(TEXT("model"), Request.Model);
	JsonObj->SetBoolField(TEXT("stream"), false);

	// Build the prompt with system context
	FString FullPrompt = Request.UserPrompt;
	JsonObj->SetStringField(TEXT("prompt"), FullPrompt);
	JsonObj->SetStringField(TEXT("system"), Request.SystemPrompt);

	// Options
	TSharedPtr<FJsonObject> Options = MakeShared<FJsonObject>();
	Options->SetNumberField(TEXT("temperature"), Request.Temperature);
	Options->SetNumberField(TEXT("num_predict"), Request.MaxTokens);
	Options->SetNumberField(TEXT("top_p"), 0.9);
	Options->SetNumberField(TEXT("repeat_penalty"), 1.1);
	JsonObj->SetObjectField(TEXT("options"), Options);

	FString OutputString;
	TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&OutputString);
	FJsonSerializer::Serialize(JsonObj.ToSharedRef(), Writer);
	return OutputString;
}

FString UOllamaClient::ParseResponseJSON(const FString& ResponseBody) const
{
	TSharedPtr<FJsonObject> JsonObj;
	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(ResponseBody);

	if (FJsonSerializer::Deserialize(Reader, JsonObj) && JsonObj.IsValid())
	{
		return JsonObj->GetStringField(TEXT("response"));
	}

	return FString();
}

void UOllamaClient::SendRequest(const FLLMRequest& Request, FOnLLMResponseComplete OnComplete, FOnLLMError OnError)
{
	if (!bServerAvailable)
	{
		OnError.ExecuteIfBound(TEXT("Ollama server not available"));
		return;
	}

	FString URL = FString::Printf(TEXT("%s:%d/api/generate"), *ServerHost, ServerPort);
	FString Body = BuildRequestJSON(Request);

	TSharedRef<IHttpRequest, ESPMode::ThreadSafe> HttpRequest = FHttpModule::Get().CreateRequest();
	HttpRequest->SetURL(URL);
	HttpRequest->SetVerb(TEXT("POST"));
	HttpRequest->SetHeader(TEXT("Content-Type"), TEXT("application/json"));
	HttpRequest->SetContentAsString(Body);
	HttpRequest->SetTimeout(10.0f);

	double StartTime = FPlatformTime::Seconds();

	HttpRequest->OnProcessRequestComplete().BindLambda(
		[this, OnComplete, OnError, StartTime](FHttpRequestPtr Req, FHttpResponsePtr Resp, bool bSuccess)
		{
			HandleResponse(Req, Resp, bSuccess, OnComplete, OnError, StartTime);
		});

	HttpRequest->ProcessRequest();

	UE_LOG(LogTemp, Verbose, TEXT("OllamaClient — Request sent to %s"), *URL);
}

void UOllamaClient::HandleResponse(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bConnectedSuccessfully,
	FOnLLMResponseComplete OnComplete, FOnLLMError OnError, double StartTime)
{
	LastResponse = FLLMResponse();
	LastResponse.GenerationTimeSeconds = FPlatformTime::Seconds() - StartTime;

	if (!bConnectedSuccessfully || !Response.IsValid())
	{
		LastResponse.bSuccess = false;
		LastResponse.ErrorMessage = TEXT("Connection failed");
		OnError.ExecuteIfBound(LastResponse.ErrorMessage);
		UE_LOG(LogTemp, Warning, TEXT("OllamaClient — Request failed: connection error"));
		return;
	}

	int32 Code = Response->GetResponseCode();
	if (Code != 200)
	{
		LastResponse.bSuccess = false;
		LastResponse.ErrorMessage = FString::Printf(TEXT("HTTP %d"), Code);
		OnError.ExecuteIfBound(LastResponse.ErrorMessage);
		UE_LOG(LogTemp, Warning, TEXT("OllamaClient — Request failed: HTTP %d"), Code);
		return;
	}

	FString ResponseText = ParseResponseJSON(Response->GetContentAsString());
	if (ResponseText.IsEmpty())
	{
		LastResponse.bSuccess = false;
		LastResponse.ErrorMessage = TEXT("Empty or unparseable response");
		OnError.ExecuteIfBound(LastResponse.ErrorMessage);
		return;
	}

	LastResponse.bSuccess = true;
	LastResponse.Text = ResponseText.TrimStartAndEnd();
	LastResponse.TokenCount = LastResponse.Text.Len() / 4; // Rough estimate

	UE_LOG(LogTemp, Log, TEXT("OllamaClient — Response (%.2fs, ~%d tokens): %s"),
		LastResponse.GenerationTimeSeconds, LastResponse.TokenCount, *LastResponse.Text);

	OnComplete.ExecuteIfBound(LastResponse.Text);
}
