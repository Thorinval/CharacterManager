namespace CharacterManager.Server.Models;

public class Action
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public enum ActionType
{
    Actif,
    Passif
}

public enum ActionTarget
{
    Ennemi,
    Allie,
    Commandant,
    Soi
}

public enum ActionDeclencheur
{
    SurAttaque,
    SurDéfense,
    SurApparition,
    SurDisparition,
    SurAttaqueCommandant
}