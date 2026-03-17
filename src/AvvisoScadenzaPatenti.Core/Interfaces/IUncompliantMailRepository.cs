namespace AvvisoScadenzaPatenti.Core.Interfaces;

using AvvisoScadenzaPatenti.Core.Models;

public interface IUncompliantMailRepository
{
    UncompliantMail? GetByName(string firstName, string lastName);
}