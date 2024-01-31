import torch.nn as nn
from torch.nn import functional as F
import torchvision.models as models

class TripletModel(nn.Module):
    def __init__(self, embedding_dimension=512, pretrained=True, model_name="resnet18", use_pretrained=True):
        super(TripletModel, self).__init__()

        self.embedding_size = embedding_dimension
        
        # ResNet
        if model_name == "resnet18":
            self.model = models.resnet18(pretrained=pretrained)
        elif model_name == "resnet34":
            self.model = models.resnet34(pretrained=pretrained)
        elif model_name == "resnet50":
            self.model = models.resnet50(pretrained=pretrained)
        elif model_name == "resnet101":
            self.model = models.resnet101(pretrained=pretrained)
        elif model_name == "resnet152":
            self.model = models.resnet152(pretrained=pretrained)

        # EfficeintNet
        elif model_name == "efficientnet_b0":
            self.model = models.efficientnet_b0(pretrained=pretrained)
        elif model_name == "efficientnet_b1":
            self.model = models.efficientnet_b1(pretrained=pretrained)
        elif model_name == "efficientnet_b2":
            self.model = models.efficientnet_b2(pretrained=pretrained)
        elif model_name == "efficientnet_b3":
            self.model = models.efficientnet_b3(pretrained=pretrained)
        elif model_name == "efficientnet_b4":
            self.model = models.efficientnet_b4(pretrained=pretrained)
        elif model_name == "efficientnet_b5":
            self.model = models.efficientnet_b5(pretrained=pretrained)
        elif model_name == "efficientnet_b6":
            self.model = models.efficientnet_b6(pretrained=pretrained)
        elif model_name == "efficientnet_b7":
            self.model = models.efficientnet_b7(pretrained=pretrained) # 84.1 acc

        # VIT
        elif model_name == "vit_b_16":
            self.model = models.vit_b_16(pretrained=pretrained)
        elif model_name == "vit_b_32":
            self.model = models.vit_b_32(pretrained=pretrained)
        elif model_name == "vit_l_16":
            self.model = models.vit_l_16(pretrained=pretrained) # maybe this
        elif model_name == "vit_l_32":
            self.model = models.vit_l_32(pretrained=pretrained)

        # ConvNext
        elif model_name == "convnext_tiny":
            self.model = models.convnext_tiny(pretrained=pretrained)
        elif model_name == "convnext_small":
            self.model = models.convnext_small(pretrained=pretrained)
        elif model_name == "convnext_base":
            self.model = models.convnext_base(pretrained=pretrained) # near large but half the size and acc 84.1
        elif model_name == "convnext_large":
            self.model = models.convnext_large(pretrained=pretrained)
            
        # For Video
        # Swin Transformer
        elif model_name == "swin_s":
            self.model = models.swin_s(pretrained=pretrained)
        elif model_name == "swin_b":
            self.model = models.swin_b(pretrained=pretrained)
        elif model_name == "swin_t":
            self.model = models.swin_t(pretrained=pretrained)
        # Swin Transformer v2
        elif model_name == "swin_v2_s":
            self.model = models.swin_v2_s(pretrained=pretrained)
        elif model_name == "swin_v2_b":
            self.model = models.swin_v2_b(pretrained=pretrained)
        elif model_name == "swin_v2_t":
            self.model = models.swin_v2_t(pretrained=pretrained)
            
        # MVIT
        elif model_name == "mvit_v1_b":
            self.model = models.video.mvit_v2_s(pretrained=pretrained)



        if model_name.startswith("resnet"):
            # Output embedding
            input_features_fc_layer = self.model.fc.in_features
            print(self.model.fc)

            if(use_pretrained):
                self.embedding_size = self.model.fc.out_features
                return

            self.model.fc = nn.Linear(input_features_fc_layer, embedding_dimension, bias=False)

            self.embedding_size = embedding_dimension

        # TODO Check corrent idx -> maybe manually find the FC layer
        elif model_name.startswith("efficientnet"):
            # Output embedding
            input_features_fc_layer = self.model.classifier.__getitem__(1).in_features
            if(use_pretrained):
                self.embedding_size = self.model.classifier.__getitem__(1).out_features
                return

            print(self.model.classifier.__getitem__(1))
            print("input_features_fc_layer: ", input_features_fc_layer)
            self.model.classifier.__setitem__(1, nn.Linear(input_features_fc_layer, embedding_dimension, bias=False))

        elif model_name.startswith("vit"):
            # Output embedding
            input_features_fc_layer = self.model.heads.__getitem__(0).in_features

            if(use_pretrained):
                self.embedding_size = self.model.heads.__getitem__(0).out_features
                return

            print(self.model.heads.__getitem__(0))
            print("input_features_fc_layer: ", input_features_fc_layer)
            self.model.heads.__setitem__(0, nn.Linear(input_features_fc_layer, embedding_dimension, bias=False))
        
        elif model_name.startswith("convnext"):
            # Output embedding
            input_features_fc_layer = self.model.classifier.__getitem__(2).in_features

            if(use_pretrained):
                self.embedding_size = self.model.classifier.__getitem__(2).out_features
                return

            print(self.model.classifier.__getitem__(2))
            print("input_features_fc_layer: ", input_features_fc_layer)
            self.model.classifier.__setitem__(2, nn.Linear(input_features_fc_layer, embedding_dimension, bias=False))
            
        elif model_name.startswith("swin"):
            # Output embedding
            input_features_fc_layer = self.model.head.in_features

            if(use_pretrained):
                self.embedding_size = self.model.head.out_features
                return

            print(self.model.head)
            print("input_features_fc_layer: ", input_features_fc_layer)
            self.model.head = nn.Linear(input_features_fc_layer, embedding_dimension, bias=False)
            
        elif model_name.startswith("mvit"):
            # Output embedding
            input_features_fc_layer = self.model.head.in_features

            if(use_pretrained):
                self.embedding_size = self.model.head.out_features
                return

            print(self.model.head)
            print("input_features_fc_layer: ", input_features_fc_layer)
            self.model.head = nn.Linear(input_features_fc_layer, embedding_dimension, bias=False)


    def forward(self, images):
        """Forward pass to output the embedding vector (feature vector) after l2-normalization."""
        embedding = self.model(images)
        # From: https://github.com/timesler/facenet-pytorch/blob/master/models/inception_resnet_v1.py#L301
        embedding = F.normalize(embedding, p=2, dim=1)

        return embedding