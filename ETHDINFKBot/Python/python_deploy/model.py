import torch.nn as nn
from torch.nn import functional as F
import torchvision.models as models

class EmbedModel(nn.Module):
    def __init__(self, model_name="resnet18"):
        super(EmbedModel, self).__init__()
        
        # ResNet
        if model_name == "resnet18":
            self.model = models.resnet18(pretrained=True)

        elif model_name == "resnet34":
            self.model = models.resnet34(pretrained=True)
        elif model_name == "resnet50":
            self.model = models.resnet50(pretrained=True)
        elif model_name == "resnet101":
            self.model = models.resnet101(pretrained=True)
        elif model_name == "resnet152":
            self.model = models.resnet152(pretrained=True)

        # EfficeintNet
        elif model_name == "efficientnet_b0":
            self.model = models.efficientnet_b0(pretrained=True)
        elif model_name == "efficientnet_b1":
            self.model = models.efficientnet_b1(pretrained=True)
        elif model_name == "efficientnet_b2":
            self.model = models.efficientnet_b2(pretrained=True)
        elif model_name == "efficientnet_b3":
            self.model = models.efficientnet_b3(pretrained=True)
        elif model_name == "efficientnet_b4":
            self.model = models.efficientnet_b4(pretrained=True)
        elif model_name == "efficientnet_b5":
            self.model = models.efficientnet_b5(pretrained=True)
        elif model_name == "efficientnet_b6":
            self.model = models.efficientnet_b6(pretrained=True)
        elif model_name == "efficientnet_b7":
            self.model = models.efficientnet_b7(pretrained=True) # 84.1 acc

        # VIT
        elif model_name == "vit_b_16":
            self.model = models.vit_b_16(pretrained=True)
        elif model_name == "vit_b_32":
            self.model = models.vit_b_32(pretrained=True)
        elif model_name == "vit_l_16":
            self.model = models.vit_l_16(pretrained=True) # maybe this
        elif model_name == "vit_l_32":
            self.model = models.vit_l_32(pretrained=True)

        # ConvNext
        elif model_name == "convnext_tiny":
            self.model = models.convnext_tiny(pretrained=True)
        elif model_name == "convnext_small":
            self.model = models.convnext_small(pretrained=True)
        elif model_name == "convnext_base":
            self.model = models.convnext_base(pretrained=True) # near large but half the size and acc 84.1
        elif model_name == "convnext_large":
            self.model = models.convnext_large(pretrained=True)
            
        # For Video
        # Swin Transformer
        elif model_name == "swin_s":
            self.model = models.swin_s(pretrained=True)
        elif model_name == "swin_b":
            self.model = models.swin_b(pretrained=True)
        elif model_name == "swin_t":
            self.model = models.swin_t(pretrained=True)
        # Swin Transformer v2
        elif model_name == "swin_v2_s":
            self.model = models.swin_v2_s(pretrained=True)
        elif model_name == "swin_v2_b":
            self.model = models.swin_v2_b(pretrained=True)
        elif model_name == "swin_v2_t":
            self.model = models.swin_v2_t(pretrained=True)
            
        # MVIT
        elif model_name == "mvit_v1_b":
            self.model = models.video.mvit_v2_s(pretrained=True)



        if model_name.startswith("resnet"):
            # Remove the classification layer
            self.model = nn.Sequential(*list(self.model.children())[:-1])


        # TODO Check corrent idx -> maybe manually find the FC layer
        elif model_name.startswith("efficientnet"):
            # Remove the classification layer
            self.model = nn.Sequential(*list(self.model.children())[:-1])

        elif model_name.startswith("vit"):
            # Remove the classification layer
            self.model = nn.Sequential(*list(self.model.children())[:-1])
        
        elif model_name.startswith("convnext"):
            # Remove the classification layer
            self.model = nn.Sequential(*list(self.model.children())[:-1])
            
        elif model_name.startswith("swin"):
            # Output embedding 
            print(self.model.head)
            
        elif model_name.startswith("mvit"):
            # Output embedding
            print(self.model.head)


    def forward(self, images):
        """Forward pass to output the embedding vector (feature vector) after l2-normalization."""
        embedding = self.model(images)
        # From: https://github.com/timesler/facenet-pytorch/blob/master/models/inception_resnet_v1.py#L301
        embedding = F.normalize(embedding, p=2, dim=1)

        return embedding