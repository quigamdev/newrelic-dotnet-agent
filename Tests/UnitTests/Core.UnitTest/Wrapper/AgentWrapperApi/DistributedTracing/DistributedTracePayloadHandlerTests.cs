﻿using JetBrains.Annotations;
using NewRelic.Agent.Configuration;
using NewRelic.Agent.Core.AgentHealth;
using NewRelic.Agent.Core.DistributedTracing;
using NewRelic.Agent.Core.Utilities;
using NewRelic.Agent.Core.Wrapper.AgentWrapperApi.Builders;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.SystemExtensions.Collections.Generic;
using NewRelic.Testing.Assertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Telerik.JustMock;
using ITransaction = NewRelic.Agent.Core.Wrapper.AgentWrapperApi.Builders.ITransaction;

namespace NewRelic.Agent.Core.Wrapper.AgentWrapperApi.DistributedTracing
{
	[TestFixture]
	public class DistributedTracePayloadHandlerTests
	{
		private const string DistributedTraceHeaderName = "NewRelic";

		private const string DtTypeApp = "App";
		private const string IncomingDtType = "Mobile";
		private const string AgentAccountId = "273070";
		private const string IncomingAccountId = "222222";
		private const string AgentApplicationId = "238575";
		private const string IncomingApplicationId = "888888";
		private const string IncomingDtGuid = "incomingGuid";
		private const string IncomingDtTraceId = "incomingTraceId";
		private const string IncomingTrustKey = "12345";
		private const string DtTransportType = "scirocco";
		private const float Priority = 0.5f;
		private const float IncomingPriority = 0.75f;
		private const string TransactionId = "transactionId";

		private DistributedTracePayloadHandler _distributedTracePayloadHandler;
		private IConfiguration _configuration;
		private IAdaptiveSampler _adaptiveSampler;
		private IAgentHealthReporter _agentHealthReporter;

		[SetUp]
		public void SetUp()
		{
			_configuration = Mock.Create<IConfiguration>();
			_adaptiveSampler = Mock.Create<IAdaptiveSampler>();

			Mock.Arrange(() => _configuration.DistributedTracingEnabled).Returns(true);
			Mock.Arrange(() => _configuration.TransactionEventsEnabled).Returns(true);
			Mock.Arrange(() => _configuration.AccountId).Returns(AgentAccountId);
			Mock.Arrange(() => _configuration.PrimaryApplicationId).Returns(AgentApplicationId);
			Mock.Arrange(() => _configuration.TrustedAccountKey).Returns(IncomingTrustKey);

			var configurationService = Mock.Create<IConfigurationService>();
			Mock.Arrange(() => configurationService.Configuration).Returns(_configuration);
			
			_agentHealthReporter = Mock.Create<IAgentHealthReporter>();
			_distributedTracePayloadHandler = new DistributedTracePayloadHandler(configurationService, _agentHealthReporter, _adaptiveSampler);
		}

		#region Accept Incoming Request

		[Test]
		public void TryDecodeInboundRequestHeaders_ReturnsValidPayload_IfHeadersAreValid()
		{
			// Arrange
			var distributedTracePayload = new DistributedTracePayload
			{
				Type = IncomingDtType,
				AccountId = IncomingAccountId,
				AppId = AgentApplicationId,
				TraceId = IncomingDtTraceId,
				TrustKey = IncomingTrustKey,
				Priority = IncomingPriority,
				Sampled = false,
				Timestamp = DateTime.UtcNow,
				TransactionId = TransactionId
			};

			var encodedPayload = HeaderEncoder.SerializeAndEncodeDistributedTracePayload(distributedTracePayload);
			var headers = new Dictionary<string, string>
			{
				{ DistributedTraceHeaderName, encodedPayload }
			};

			// Act
			var payload = _distributedTracePayloadHandler.TryDecodeInboundRequestHeaders(headers);

			// Assert
			Assert.IsNotNull(payload);

			NrAssert.Multiple(
				() => Assert.AreEqual(IncomingDtType, payload.Type),
				() => Assert.AreEqual(IncomingAccountId, payload.AccountId),
				() => Assert.AreEqual(AgentApplicationId, payload.AppId),
				() => Assert.AreEqual(null, payload.Guid),
				() => Assert.AreEqual(IncomingTrustKey, payload.TrustKey),
				() => Assert.AreEqual(IncomingPriority, payload.Priority),
				() => Assert.AreEqual(false, payload.Sampled),
				() => Assert.That(payload.Timestamp, Is.LessThan(DateTime.UtcNow)),
				() => Assert.AreEqual(TransactionId, payload.TransactionId)
			);
		}

		[Test]
		public void PayloadShouldBeNullWhenTrustKeyNotTrusted()
		{
			// Arrange
			Mock.Arrange(() => _configuration.TrustedAccountKey).Returns("NOPE");

			var distributedTracePayload = new DistributedTracePayload
			{
				Type = IncomingDtType,
				AccountId = IncomingAccountId,
				AppId = AgentApplicationId,
				TraceId = IncomingDtTraceId,
				TrustKey = IncomingTrustKey,
				Priority = IncomingPriority,
				Sampled = false,
				Timestamp = DateTime.UtcNow,
				TransactionId = TransactionId
			};

			var encodedPayload = HeaderEncoder.SerializeAndEncodeDistributedTracePayload(distributedTracePayload);
			var headers = new Dictionary<string, string>
			{
				{ DistributedTraceHeaderName, encodedPayload }
			};

			// Act
			var payload = _distributedTracePayloadHandler.TryDecodeInboundRequestHeaders(headers);

			// Assert
			Assert.IsNull(payload);
		}

		[Test]
		public void PayloadShouldBePopulatedWhenTrustKeyTrusted()
		{
			// Arrange
			var distributedTracePayload = new DistributedTracePayload
			{
				Type = IncomingDtType,
				AccountId = IncomingAccountId,
				AppId = AgentApplicationId,
				TraceId = IncomingDtTraceId,
				TrustKey = IncomingTrustKey,
				Priority = IncomingPriority,
				Sampled = false,
				Timestamp = DateTime.UtcNow,
				TransactionId = TransactionId
			};

			var encodedPayload = HeaderEncoder.SerializeAndEncodeDistributedTracePayload(distributedTracePayload);
			var headers = new Dictionary<string, string>
			{
				{ DistributedTraceHeaderName, encodedPayload }
			};

			// Act
			var payload = _distributedTracePayloadHandler.TryDecodeInboundRequestHeaders(headers);

			// Assert
			Assert.IsNotNull(payload);
		}

		[Test]
		public void PayloadShouldBeNullWhenTrustKeyNullAndAccountIdNotTrusted()
		{
			// Arrange
			Mock.Arrange(() => _configuration.TrustedAccountKey).Returns("NOPE");

			var distributedTracePayload = new DistributedTracePayload
			{
				Type = IncomingDtType,
				AccountId = IncomingAccountId,
				AppId = AgentApplicationId,
				TraceId = IncomingDtTraceId,
				TrustKey = null,
				Priority = IncomingPriority,
				Sampled = false,
				Timestamp = DateTime.UtcNow,
				TransactionId = TransactionId
			};

			var encodedPayload = HeaderEncoder.SerializeAndEncodeDistributedTracePayload(distributedTracePayload);
			var headers = new Dictionary<string, string>
			{
				{ DistributedTraceHeaderName, encodedPayload }
			};

			// Act
			var payload = _distributedTracePayloadHandler.TryDecodeInboundRequestHeaders(headers);

			// Assert
			Assert.IsNull(payload);
		}

		[Test]
		public void PayloadShouldBePopulatedWhenTrustKeyNullAndAccountIdTrusted()
		{
			// Arrange
			Mock.Arrange(() => _configuration.TrustedAccountKey).Returns(IncomingAccountId);

			var distributedTracePayload = new DistributedTracePayload
			{
				Type = IncomingDtType,
				AccountId = IncomingAccountId,
				AppId = AgentApplicationId,
				TraceId = IncomingDtTraceId,
				TrustKey = null,
				Priority = IncomingPriority,
				Sampled = false,
				Timestamp = DateTime.UtcNow,
				TransactionId = TransactionId
			};

			var encodedPayload = HeaderEncoder.SerializeAndEncodeDistributedTracePayload(distributedTracePayload);
			var headers = new Dictionary<string, string>
			{
				{ DistributedTraceHeaderName, encodedPayload }
			};

			// Act
			var payload = _distributedTracePayloadHandler.TryDecodeInboundRequestHeaders(headers);

			// Assert
			Assert.IsNotNull(payload);
		}


		[Test]
		public void TryDecodeInboundRequestHeaders_ReturnsNull_IfHigherMajorVersion()
		{
			// Arrange
			var distributedTracePayload = new DistributedTracePayload
			{
				Version = new [] { int.MaxValue, 1 },
				Type = IncomingDtType,
				AccountId = IncomingAccountId,
				AppId = AgentApplicationId,
				TraceId = IncomingDtTraceId,
				TrustKey = IncomingTrustKey,
				Priority = IncomingPriority,
				Sampled = false,
				Timestamp = DateTime.UtcNow,
				TransactionId = TransactionId
			};

			var encodedPayload = HeaderEncoder.SerializeAndEncodeDistributedTracePayload(distributedTracePayload);
			var headers = new Dictionary<string, string>
			{
				{ DistributedTraceHeaderName, encodedPayload }
			};

			// Act
			var payload = _distributedTracePayloadHandler.TryDecodeInboundRequestHeaders(headers);

			// Assert
			Assert.IsNull(payload);
		}

		[Test]
		public void TryDecodeInboundRequestHeaders_ReturnsValidPayload_IfHeaderNameCamelCase()
		{
			// Arrange
			var distributedTracePayload = new DistributedTracePayload
			{
				Type = IncomingDtType,
				AccountId = IncomingAccountId,
				AppId = AgentApplicationId,
				TraceId = IncomingDtTraceId,
				TrustKey = IncomingTrustKey,
				Priority = IncomingPriority,
				Sampled = false,
				Timestamp = DateTime.UtcNow,
				TransactionId = TransactionId
			};

			var encodedPayload = HeaderEncoder.SerializeAndEncodeDistributedTracePayload(distributedTracePayload);
			var headers = new Dictionary<string, string>
			{
				{ DistributedTraceHeaderName, encodedPayload }
			};

			// Act
			var payload = _distributedTracePayloadHandler.TryDecodeInboundRequestHeaders(headers);

			// Assert
			Assert.NotNull(payload);
		}

		[Test]
		public void TryDecodeInboundRequestHeaders_ReturnsValidPayload_IfHeaderNameLowerCase()
		{
			// Arrange
			var distributedTracePayload = new DistributedTracePayload
			{
				Type = IncomingDtType,
				AccountId = IncomingAccountId,
				AppId = AgentApplicationId,
				TraceId = IncomingDtTraceId,
				TrustKey = IncomingTrustKey,
				Priority = IncomingPriority,
				Sampled = false,
				Timestamp = DateTime.UtcNow,
				TransactionId = TransactionId
			};

			var encodedPayload = HeaderEncoder.SerializeAndEncodeDistributedTracePayload(distributedTracePayload);
			var headers = new Dictionary<string, string>
			{
				{ DistributedTraceHeaderName.ToLower(), encodedPayload }
			};
			
			// Act
			var payload = _distributedTracePayloadHandler.TryDecodeInboundRequestHeaders(headers);

			// Assert
			Assert.NotNull(payload);
		}

		[Test]
		public void TryDecodeInboundRequestHeaders_ReturnsValidPayload_IfHeaderNameUpperCase()
		{
			// Arrange
			var distributedTracePayload = new DistributedTracePayload
			{
				Type = IncomingDtType,
				AccountId = IncomingAccountId,
				AppId = AgentApplicationId,
				TraceId = IncomingDtTraceId,
				TrustKey = IncomingTrustKey,
				Priority = IncomingPriority,
				Sampled = false,
				Timestamp = DateTime.UtcNow,
				TransactionId = TransactionId
			};

			var encodedPayload = HeaderEncoder.SerializeAndEncodeDistributedTracePayload(distributedTracePayload);
			var headers = new Dictionary<string, string>
			{
				{ DistributedTraceHeaderName.ToUpper(), encodedPayload }
			};
			
			// Act
			var payload = _distributedTracePayloadHandler.TryDecodeInboundRequestHeaders(headers);

			// Assert
			Assert.NotNull(payload);
		}

		[Test]
		public void ShouldNotCreatePayloadWhenGuidAndTransactionIdNull()
		{
			// Arrange
			var distributedTracePayload = new DistributedTracePayload
			{
				Type = IncomingDtType,
				AccountId = IncomingAccountId,
				AppId = AgentApplicationId,
				TraceId = IncomingDtTraceId,
				TrustKey = IncomingTrustKey,
				Priority = IncomingPriority,
				Sampled = false,
				Timestamp = DateTime.UtcNow,
				Guid = null,
				TransactionId = null
			};

			var encodedPayload = HeaderEncoder.SerializeAndEncodeDistributedTracePayload(distributedTracePayload);
			var headers = new Dictionary<string, string>
			{
				{ DistributedTraceHeaderName, encodedPayload }
			};

			// Act
			var payload = _distributedTracePayloadHandler.TryDecodeInboundRequestHeaders(headers);

			// Assert
			Assert.IsNull(payload);
		}

		[Test]
		public void ShouldGenerateParseExceptionMetricWhenGuidAndTransactionIdNull()
		{
			// Arrange
			var distributedTracePayload = new DistributedTracePayload
			{
				Type = IncomingDtType,
				AccountId = IncomingAccountId,
				AppId = AgentApplicationId,
				TraceId = IncomingDtTraceId,
				TrustKey = IncomingTrustKey,
				Priority = IncomingPriority,
				Sampled = false,
				Timestamp = DateTime.UtcNow,
				Guid = null,
				TransactionId = null
			};

			var encodedPayload = HeaderEncoder.SerializeAndEncodeDistributedTracePayload(distributedTracePayload);
			var headers = new Dictionary<string, string>
			{
				{ DistributedTraceHeaderName, encodedPayload }
			};

			// Act
			_distributedTracePayloadHandler.TryDecodeInboundRequestHeaders(headers);

			// Assert
			Mock.Assert(() => _agentHealthReporter.ReportSupportabilityDistributedTraceAcceptPayloadParseException(), Occurs.Once());
		}


		#endregion Accept Incoming Request

		#region Create Outbound Request

		[Test]
		public void TryGetOutboundRequestHeaders_ReturnsCorrectHeaders_IfFirstInChain()
		{
			// Arrange
			var transaction = BuildMockTransaction(hasIncomingPayload: false);

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];

			DistributedTracePayload dtPayload = null;
			Assert.That(() => dtPayload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson), Is.Not.Null);

			// Assert
			NrAssert.Multiple(
				() => Assert.AreEqual(DtTypeApp, dtPayload.Type),
				() => Assert.AreEqual(AgentAccountId, dtPayload.AccountId),
				() => Assert.AreEqual(AgentApplicationId, dtPayload.AppId),
				() => Assert.AreEqual(null, dtPayload.Guid),
				() => Assert.AreEqual(transaction.Guid, dtPayload.TraceId),
				() => Assert.AreEqual(Priority, dtPayload.Priority),
				() => Assert.That(dtPayload.Timestamp, Is.LessThan(DateTime.UtcNow)),
				() => Assert.AreEqual($"{transaction.Guid}", dtPayload.TransactionId)
			);
		}

		[Test]
		public void TryGetOutboundRequestHeaders_ReturnsCorrectHeaders_IfNotFirstInChain()
		{
			// Arrange
			Mock.Arrange(() => _configuration.TrustedAccountKey).Returns(IncomingTrustKey);
			var transaction = BuildMockTransaction(hasIncomingPayload: true);

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];

			DistributedTracePayload dtPayload = null;
			Assert.That(() => dtPayload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson), Is.Not.Null);

			// Assert
			NrAssert.Multiple(
				() => Assert.AreEqual(DtTypeApp, dtPayload.Type),
				() => Assert.AreEqual(AgentAccountId, dtPayload.AccountId),
				() => Assert.AreEqual(AgentApplicationId, dtPayload.AppId),
				() => Assert.AreEqual(null, dtPayload.Guid),
				() => Assert.AreEqual(IncomingDtTraceId, dtPayload.TraceId),
				() => Assert.AreEqual(IncomingTrustKey, dtPayload.TrustKey),
				() => Assert.AreEqual(IncomingPriority, dtPayload.Priority),
				() => Assert.That(dtPayload.Timestamp, Is.LessThan(DateTime.UtcNow)),
				() => Assert.AreEqual($"{transaction.Guid}", dtPayload.TransactionId)
			);
		}

		[Test]
		public void ShouldPopulateTrustKeyWhenTrustedAccountKeyDifferentThanAccountId()
		{
			// Arrange
			var transaction = BuildMockTransaction(hasIncomingPayload: true);

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];
			var payload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson);

			// Assert
			Assert.NotNull(payload);
			Assert.AreEqual(IncomingTrustKey, payload.TrustKey);
		}

		[Test]
		public void ShouldNotPopulateTrustKeyWhenTrustedAccountKeySameAsAccountId()
		{
			// Arrange
			Mock.Arrange(() => _configuration.TrustedAccountKey).Returns(AgentAccountId);

			var transaction = BuildMockTransaction(hasIncomingPayload: true);

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];
			var payload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson);

			// Assert
			Assert.NotNull(payload);
			Assert.IsNull(payload.TrustKey);
		}
		
		[Test]
		public void PayloadShouldHaveGuidWhenSpansEnabledAndTransactionSampled()
		{
			// Arrange
			Mock.Arrange(() => _configuration.SpanEventsEnabled).Returns(true);

			var transaction = BuildMockTransaction();
			transaction.TransactionMetadata.DistributedTraceSampled = true;

			const string expectedGuid = "expectedId";
			var segment = Mock.Create<ISegment>();
			Mock.Arrange(() => segment.SpanId).Returns(expectedGuid);

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction, segment).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];
			var payload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson);

			// Assert
			Assert.NotNull(payload.Guid);
			Assert.AreEqual(expectedGuid, payload.Guid);
		}

		[Test]
		public void PayloadShouldNotHaveGuidWhenSpansDisabled()
		{
			// Arrange
			Mock.Arrange(() => _configuration.SpanEventsEnabled).Returns(false);

			var transaction = BuildMockTransaction(hasIncomingPayload: true);

			const string expectedGuid = "expectedId";
			var segment = Mock.Create<ISegment>();
			Mock.Arrange(() => segment.SpanId).Returns(expectedGuid);

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction, segment).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];
			var payload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson);

			// Assert
			Assert.IsNull(payload.Guid);
		}

		[Test]
		public void PayloadShouldNotHaveGuidWhenSpansEnabledAndTransactionNotSampled()
		{
			// Arrange
			Mock.Arrange(() => _configuration.SpanEventsEnabled).Returns(true);

			var transaction = BuildMockTransaction();
			transaction.TransactionMetadata.DistributedTraceSampled = false;

			const string expectedGuid = "expectedId";
			var segment = Mock.Create<ISegment>();
			Mock.Arrange(() => segment.SpanId).Returns(expectedGuid);

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction, segment).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];
			var payload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson);

			// Assert
			Assert.IsNull(payload.Guid);
		}
		
		[Test]
		public void ShouldNotCreatePayloadWhenAccountIdNotReceivedFromServer()
		{
			// Arrange
			Mock.Arrange(() => _configuration.AccountId).Returns<string>(null);
			Mock.Arrange(() => _configuration.PrimaryApplicationId).Returns(AgentApplicationId);

			var transaction = BuildMockTransaction(hasIncomingPayload: true);

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);

			// Assert
			var hasHeaderPayload = headers.ContainsKey(DistributedTraceHeaderName);
			Assert.False(hasHeaderPayload);
		}

		[Test]
		public void ShouldNotCreatePayloadWhenPrimaryApplicationIdNotReceivedFromServer()
		{
			// Arrange
			Mock.Arrange(() => _configuration.AccountId).Returns(AgentAccountId);
			Mock.Arrange(() => _configuration.PrimaryApplicationId).Returns<string>(null);

			var transaction = BuildMockTransaction(hasIncomingPayload: true);

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);

			// Assert
			var hasHeaderPayload = headers.ContainsKey(DistributedTraceHeaderName);
			Assert.False(hasHeaderPayload);
		}

		[Test]
		public void ShouldNotCreatePayloadWhenSampledNotSet()
		{
			// Arrange
			var transaction = BuildMockTransaction();
			transaction.TransactionMetadata.DistributedTraceSampled = null;

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);

			// Assert
			var hasHeaderPayload = headers.ContainsKey(DistributedTraceHeaderName);
			Assert.False(hasHeaderPayload);
		}

		[Test]
		public void PayloadShouldNotHaveTransactionIdWhenTransactionEventsDisabled()
		{
			// Arrange
			Mock.Arrange(() => _configuration.SpanEventsEnabled).Returns(true);
			Mock.Arrange(() => _configuration.TransactionEventsEnabled).Returns(false);
			
			var transaction = BuildMockTransaction();

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];
			var payload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson);

			// Assert
			Assert.IsNull(payload.TransactionId);
		}

		[Test]
		public void PayloadShouldHaveTransactionIdWhenTransactionEventsEnabled()
		{
			// Arrange
			Mock.Arrange(() => _configuration.TransactionEventsEnabled).Returns(true);
			
			var transaction = BuildMockTransaction();

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];
			var payload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson);

			// Assert
			Assert.NotNull(payload.TransactionId);
			Assert.AreEqual(transaction.Guid, payload.TransactionId);
		}

		[Test]
		public void ShouldNotCreatePayloadWhenSpanEventsDisabledAndTransactionEventsDisabled()
		{
			// Arrange
			Mock.Arrange(() => _configuration.SpanEventsEnabled).Returns(false);
			Mock.Arrange(() => _configuration.TransactionEventsEnabled).Returns(false);

			var transaction = BuildMockTransaction();

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);

			// Assert
			var hasHeaderPayload = headers.ContainsKey(DistributedTraceHeaderName);
			Assert.False(hasHeaderPayload);
		}

		#region TraceId Tests

		[Test]
		public void PayloadShouldHaveTraceIdWhenSpansEnabled()
		{
			// Arrange
			Mock.Arrange(() => _configuration.SpanEventsEnabled).Returns(true);

			var transaction = BuildMockTransaction(hasIncomingPayload: true);
			var segment = Mock.Create<ISegment>();

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction, segment).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];
			var payload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson);

			// Assert
			Assert.NotNull(payload.TraceId);
		}

		[Test]
		public void PayloadShouldHaveTraceIdWhenSpansDisabled()
		{
			// Arrange
			Mock.Arrange(() => _configuration.SpanEventsEnabled).Returns(false);

			var transaction = BuildMockTransaction(hasIncomingPayload: true);
			var segment = Mock.Create<ISegment>();

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction, segment).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];
			var payload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson);

			// Assert
			Assert.NotNull(payload.TraceId);
		}

		[Test]
		public void TraceIdShouldBeSameAsIncomingTraceIdWhenReceived()
		{
			var transaction = BuildMockTransaction(hasIncomingPayload: true);
			var segment = Mock.Create<ISegment>();

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction, segment).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];
			var payload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson);

			// Assert
			Assert.AreEqual(IncomingDtTraceId, payload.TraceId);
		}

		[Test]
		public void PayloadShouldHaveExpectedTraceIdValueWhenNoTraceIdReceived()
		{
			// Arrange
			var transaction = BuildMockTransaction();

			var segment = Mock.Create<ISegment>();
			var expectedTraceIdValue = transaction.Guid;

			// Act
			var headers = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction, segment).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var encodedJson = headers[DistributedTraceHeaderName];
			var payload = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(encodedJson);

			// Assert
			Assert.AreEqual(expectedTraceIdValue, payload.TraceId);
		}

		[Test]
		public void TraceIdShouldBeSameForAllSpansWhenNoTraceIdReceived()
		{
			// Arrange
			var transaction = BuildMockTransaction();

			var segment1 = Mock.Create<ISegment>();
			var segment2 = Mock.Create<ISegment>();

			// Act
			var firstHeaders = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction, segment1).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var firstEncodedJson = firstHeaders[DistributedTraceHeaderName];
			var payload1 = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(firstEncodedJson);

			var secondHeaders = _distributedTracePayloadHandler.TryGetOutboundRequestHeaders(transaction, segment2).ToDictionary(equalityComparer: StringComparer.OrdinalIgnoreCase);
			var secondEncodedJson = secondHeaders[DistributedTraceHeaderName];
			var payload2 = HeaderEncoder.TryDecodeAndDeserializeDistributedTracePayload(secondEncodedJson);

			// Assert
			Assert.AreEqual(payload1.TraceId, payload2.TraceId);
		}

		#endregion TraceID Tests

		#endregion

		#region helpers

		[NotNull]
		private static ITransaction BuildMockTransaction(bool hasIncomingPayload = false)
		{
			var transaction = Mock.Create<ITransaction>();
			var transactionMetadata = Mock.Create<TransactionMetadata>();
			Mock.Arrange(() => transaction.TransactionMetadata).Returns(transactionMetadata);
			
			var transactionGuid = Guid.NewGuid().ToString();
			Mock.Arrange(() => transaction.Guid).Returns(transactionGuid);

			transaction.TransactionMetadata.Priority = Priority;
			transaction.TransactionMetadata.DistributedTraceSampled = false;

			if (hasIncomingPayload)
			{
				transaction.TransactionMetadata.DistributedTraceType = IncomingDtType;
				transaction.TransactionMetadata.DistributedTraceAccountId = IncomingAccountId;
				transaction.TransactionMetadata.DistributedTraceAppId = IncomingApplicationId;
				transaction.TransactionMetadata.DistributedTraceGuid = IncomingDtGuid;
				transaction.TransactionMetadata.DistributedTraceTraceId = IncomingDtTraceId;
				transaction.TransactionMetadata.DistributedTraceTrustKey = IncomingTrustKey;
				transaction.TransactionMetadata.DistributedTraceTransportType = DtTransportType;
				transaction.TransactionMetadata.Priority = IncomingPriority;
			}
			else
			{
				transaction.TransactionMetadata.DistributedTraceType = null;
				transaction.TransactionMetadata.DistributedTraceAccountId = null;
				transaction.TransactionMetadata.DistributedTraceAppId = null;
				transaction.TransactionMetadata.DistributedTraceGuid = null;
				transaction.TransactionMetadata.DistributedTraceTraceId = null;
				transaction.TransactionMetadata.DistributedTraceTrustKey = null;
				transaction.TransactionMetadata.DistributedTraceTransportType = null;
			}

			return transaction;
		}

		#endregion helpers
	}
}
