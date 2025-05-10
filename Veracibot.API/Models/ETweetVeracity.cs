namespace Veracibot.API.Models
{
    //1 significa que a primeira pessoa tem plena razão e a segunda está errada, até 5 em que a primeira pessoa está totalmente errada e a segunda certa.
    public enum ETweetVeracity
    {
        Waiting = 0,
        Verdadeiro = 1,
        ParcialmenteVerdadeiro = 2,
        Neutro = 3,
        ParcialmenteFalso = 4,
        Falso = 5
    }
}
