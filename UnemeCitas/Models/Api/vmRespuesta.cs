namespace UnemeCitas.Models.Api
{
    public class vmRespuesta
    {
        public int Resultado { get; set; }
        public object? Data { get; set; }
        public string? Msg { get; set; }
        public vmRespuesta()
        {
            Resultado = 0;
            Data = null;
            Msg = "";

        }
    }
}
