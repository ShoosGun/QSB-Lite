using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SNet_Client.EntityScripts.TransfromSync
{
    public class DynamicReferenceTransformEntitySync : TransformEntitySync
    {
        protected override void Awake()
        {
            base.Awake();
            UniqueScriptIdentifingString = "DynamicReferenceTransformEntitySync";
        }

        protected virtual void FixedUpdate()
        {
            //TODO
            //Pegar a lista de reference frames e achar o mais perto
            //Se nenhum estiver dentro do limite de distancia desejado falar que ele está no espaço
            //Mas nesse caso dar uma estendida no limite de distancia para o anterior achado
            //Essa distancia vai vir do ReferenceFrameLocator
        }
    }
}
