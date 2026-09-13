using FCG.Contracts.Events;
using FCG.PaymentsAPI.Mensageria;
using FCG.PaymentsAPI.Tests.Fakes;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace FCG.PaymentsAPI.Tests.Mensageria;

public class IdempotentConsumeFilterTests
{
    private static OrderPlacedEvent CriarEvento() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Jogo Teste", 99.90m, DateTime.UtcNow);

    private static ConsumeContext<OrderPlacedEvent> CriarContexto(OrderPlacedEvent evento, Guid messageId)
    {
        var mock = new Mock<ConsumeContext<OrderPlacedEvent>>();
        mock.Setup(c => c.Message).Returns(evento);
        mock.Setup(c => c.MessageId).Returns(messageId);
        mock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock.Object;
    }

    [Fact]
    public async Task Send_PrimeiraMensagem_ChamaNext()
    {
        var filtro = new IdempotentConsumeFilter<OrderPlacedEvent>(
            new ProcessedEventStoreFake(), Mock.Of<ILogger<IdempotentConsumeFilter<OrderPlacedEvent>>>());

        var contexto = CriarContexto(CriarEvento(), Guid.NewGuid());

        var nextChamado = false;
        var pipeMock = new Mock<IPipe<ConsumeContext<OrderPlacedEvent>>>();
        pipeMock.Setup(p => p.Send(It.IsAny<ConsumeContext<OrderPlacedEvent>>()))
            .Callback(() => nextChamado = true)
            .Returns(Task.CompletedTask);

        await filtro.Send(contexto, pipeMock.Object);

        nextChamado.Should().BeTrue();
    }

    [Fact]
    public async Task Send_SegundaMensagemComMesmoMessageId_NaoChamaNext()
    {
        var store = new ProcessedEventStoreFake();
        var filtro = new IdempotentConsumeFilter<OrderPlacedEvent>(
            store, Mock.Of<ILogger<IdempotentConsumeFilter<OrderPlacedEvent>>>());

        var evento = CriarEvento();
        var messageId = Guid.NewGuid();

        var chamadasAoNext = 0;
        var pipeMock = new Mock<IPipe<ConsumeContext<OrderPlacedEvent>>>();
        pipeMock.Setup(p => p.Send(It.IsAny<ConsumeContext<OrderPlacedEvent>>()))
            .Callback(() => chamadasAoNext++)
            .Returns(Task.CompletedTask);

        await filtro.Send(CriarContexto(evento, messageId), pipeMock.Object);
        await filtro.Send(CriarContexto(evento, messageId), pipeMock.Object);

        chamadasAoNext.Should().Be(1);
    }
}
